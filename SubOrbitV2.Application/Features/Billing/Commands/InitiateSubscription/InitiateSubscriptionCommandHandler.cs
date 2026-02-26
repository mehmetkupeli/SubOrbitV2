using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Application.Common.Models.Billing;
using SubOrbitV2.Application.Common.Models.Payment;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Enums;
using SubOrbitV2.Domain.Specifications.Billing;
using SubOrbitV2.Domain.Specifications.Catalog;

namespace SubOrbitV2.Application.Features.Billing.Commands.InitiateSubscription;

public class InitiateSubscriptionCommandHandler : IRequestHandler<InitiateSubscriptionCommand, Result<InitiateSubscriptionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectContext _projectContext;
    private readonly IPricingCalculatorService _pricingCalculator;
    private readonly INexiClient _nexiClient;

    public InitiateSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        IProjectContext projectContext,
        IPricingCalculatorService pricingCalculator,
        INexiClient nexiClient)
    {
        _unitOfWork = unitOfWork;
        _projectContext = projectContext;
        _pricingCalculator = pricingCalculator;
        _nexiClient = nexiClient;
    }

    public async Task<Result<InitiateSubscriptionResponse>> Handle(InitiateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var projectId = _projectContext.ProjectId;
        var currentProject = _projectContext.CurrentProject;
        try
        {

            #region 1. Price ve Coupon Validasyonu
            var price = await _unitOfWork.Repository<Price>().GetByIdAsync(request.PriceId);
            if (price == null || price.ProjectId != projectId || !price.IsActive)
                return Result<InitiateSubscriptionResponse>.Failure("Geçersiz paket seçimi.");

            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(price.ProductId);

            Coupon? coupon = null;
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                var couponSpec = new CouponByCodeSpecification(projectId, request.CouponCode);
                coupon = await _unitOfWork.Repository<Coupon>().GetEntityWithSpec(couponSpec);

                if (coupon == null || !coupon.IsActive || (coupon.ExpiryDate.HasValue && coupon.ExpiryDate < DateTime.UtcNow))
                    return Result<InitiateSubscriptionResponse>.Failure("Geçersiz veya süresi dolmuş kupon.");
            }
            #endregion

            #region 2. Payer (Müşteri) Draft/Upsert Kaydı
            var payerSpec = new PayerByExternalIdSpecification(projectId, request.ExternalId);
            var payer = await _unitOfWork.Repository<Payer>().GetEntityWithSpec(payerSpec);

            if (payer == null)
            {
                payer = new Payer
                {
                    ExternalId = request.ExternalId,
                    Name = request.Name,
                    Email = request.Email,
                    TaxOffice = request.TaxOffice,
                    TaxNumber = request.TaxNumber,
                    BillingAddress = request.BillingAddress,
                    City = request.City,
                    Country = request.Country,
                    Status = PayerStatus.Pending,
                    AlignmentStrategy = request.AlignmentStrategy,
                    BillingAnchorDay = request.BillingAnchorDay,
                };
                await _unitOfWork.Repository<Payer>().AddAsync(payer);
            }
            else
            {
                return Result<InitiateSubscriptionResponse>.Failure("Bu Payer zaten sistemde kayıtlı.");
            }
            #endregion

            #region 3. Fiyat Hesaplama Motoru (Kıstelyevm & Kupon)
            var pricingRequest = new PricingRequest(
                BaseAmount: price.Amount,
                VatRate: price.VatRate,
                Interval: price.Interval,
                IntervalCount: price.IntervalCount,
                AlignmentStrategy: payer.AlignmentStrategy,
                AlignmentDay: payer.BillingAnchorDay,
                CouponType: coupon?.DiscountType,
                CouponValue: coupon?.DiscountValue
            );

            var pricingResult = _pricingCalculator.Calculate(pricingRequest);
            #endregion

            #region 4. Subscription (Abonelik) Draft Kaydı
            var subscription = new Subscription
            {
                PayerId = payer.Id,
                ProductId = product!.Id,
                PriceId = price.Id,
                Status = SubscriptionStatus.Pending,
                CurrentPeriodStart = DateTime.UtcNow,
                NextBillingDate = pricingResult.NextBillingDate,
                IsMain = true,
                ActiveCouponId = coupon?.Id,
                CurrentPeriodEnd = pricingResult.NextBillingDate,
                ExternalId = request.ExternalId,
            };
            await _unitOfWork.Repository<Subscription>().AddAsync(subscription);
            #endregion

            #region 5. CouponRedemption (Kupon Rezerve Etme) Draft Kaydı
            if (coupon != null)
            {
                var couponRedemption = new CouponRedemption
                {
                    CouponId = coupon.Id,
                    PayerId = payer.Id,
                    SubscriptionId = subscription.Id,
                    RedeemedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<CouponRedemption>().AddAsync(couponRedemption);
            }
            #endregion

            #region 6. Activity Log Kaydı
            var activityLog = new SubscriptionActivityLog
            {
                SubscriptionId = subscription.Id,
                Description = $"Abonelik başlatma isteği alındı. Nexi ödemesi bekleniyor. (Tutar: {pricingResult.FinalTotal} {price.Currency})",
                CreatedAt = DateTime.UtcNow,
                Action = "Iniate Subscription"
            };
            await _unitOfWork.Repository<SubscriptionActivityLog>().AddAsync(activityLog);
            #endregion

            // Tüm taslakları tek hamlede veritabanına mühürlüyoruz
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            #region 7. Nexi Ödeme Linki Oluşturma
            // Kusursuz Referansımız: Direkt olarak SubscriptionId! (Webhook'ta bu ID ile uyanacağız)

            // Nexi İçin Integer (Kuruş) Dönüşümü
            int unitPriceInCents = (int)Math.Round(pricingResult.SubTotal * 100);
            // Sadece KDV Tutarı
            int taxAmountInCents = (int)Math.Round(pricingResult.TaxAmount * 100);
            // Müşteriden çekilecek tam tutar (Net + KDV)
            int grossAmountInCents = (int)Math.Round(pricingResult.FinalTotal * 100);
            // Satırın toplam net tutarı (Adet 1 olduğu için unitPriceInCents ile aynı)
            int netTotalAmountInCents = (int)Math.Round(pricingResult.SubTotal * 100);
            // Vergi Oranı Basis Points (Örn: %25 KDV -> 25 * 100 = 2500)
            int taxRateBasisPoints = (int)Math.Round(price.VatRate * 100);

            var subId = $"SUB-{subscription.Id}";

            var nexiOrderItemDto = new NexiOrderItemDto
            {
                NexiReference = price.Id.ToString(),
                Name = price.Name,
                Quantity = 1,
                Unit = "pcs",
                UnitPrice = unitPriceInCents,
                TaxRate = taxRateBasisPoints,
                TaxAmount = taxAmountInCents,
                GrossTotalAmount = grossAmountInCents,
                NetTotalAmount = netTotalAmountInCents
            };

            #endregion

            try
            {
                var nexiPaymentResponse = await _nexiClient.InitializePaymentAsync(nexiOrderItemDto);

                return Result<InitiateSubscriptionResponse>.Success(new InitiateSubscriptionResponse(subscription.Id, nexiPaymentResponse.HostedPaymentPageUrl), "Abonelik oluşturuldu ve ödeme sayfası linki alındı.");
            }
            catch (Exception nexiEx)
            {
                subscription.Status = SubscriptionStatus.Canceled;
                await _unitOfWork.SaveChangesAsync(cancellationToken); 
                return Result<InitiateSubscriptionResponse>.Failure("Ödeme altyapısına geçici olarak ulaşılamıyor.");
            }

        }
        catch (Exception ex)
        {
            return Result<InitiateSubscriptionResponse>.Failure("İşlem sırasında beklenmeyen bir hata oluştu.");
        }
    }
}