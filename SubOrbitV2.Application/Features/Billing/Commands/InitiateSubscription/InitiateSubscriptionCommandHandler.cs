using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Application.Common.Models.Billing;
using SubOrbitV2.Application.Common.Models.Payment;
using SubOrbitV2.Application.Common.Utils;
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
            var payerSpec = new PayerWithSubscriptionsByExternalIdSpecification(projectId, request.ExternalId);
            var payer = await _unitOfWork.Repository<Payer>().GetEntityWithSpec(payerSpec);

            if (payer != null)
            {
                // Durum A: Müşterinin zaten aktif bir ana aboneliği varsa ENGELLE
                var hasActiveMainSubscription = payer.Subscriptions.Any(s => s.IsMain && s.Status == SubscriptionStatus.Active);
                if (hasActiveMainSubscription)
                {
                    return Result<InitiateSubscriptionResponse>.Failure("Bu kullanıcının zaten aktif bir aboneliği bulunmaktadır.");
                }

                // Durum B: Aktif aboneliği yok (Sepeti terk etmiş, iptal etmiş vs.)
                payer.Name = request.Name;
                payer.Email = request.Email;
                payer.TaxOffice = request.TaxOffice;
                payer.TaxNumber = request.TaxNumber;
                payer.BillingAddress = request.BillingAddress;
                payer.City = request.City;
                payer.Country = request.Country;
                payer.AlignmentStrategy = request.AlignmentStrategy;
                payer.BillingAnchorDay = request.BillingAnchorDay;

                // Kaydı güncelleyip sürece devam ediyoruz
                _unitOfWork.Repository<Payer>().Update(payer);
            }
            else
            {
                // Hiç kaydı yoksa sıfırdan taslak (Pending) oluşturuyoruz
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
                Label = payer.Name,
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

            #region 6. Fatura (Invoice) ve Satır (InvoiceLine) Draft Kaydı
            var invoiceNumber = InvoiceHelper.GenerateInvoiceNumber();
            var invoice = new Invoice
            {
                ProjectId = projectId,
                PayerId = payer.Id,
                Number = invoiceNumber,
                BillingReason = InvoiceBillingReason.Manual,
                // Snapshot alanları dolduruluyor
                CustomerName = payer.Name,
                CustomerEmail = payer.Email,
                CustomerTaxOffice = payer.TaxOffice,
                CustomerTaxNumber = payer.TaxNumber,
                CustomerAddress = payer.BillingAddress,
                CustomerCity = payer.City,
                CustomerCountry = payer.Country,
                // Tarihler
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = pricingResult.NextBillingDate,
                DueDate = DateTime.UtcNow, 
                // Finansallar
                Currency = price.Currency,
                Subtotal = pricingResult.ProratedAmount,
                TotalDiscount = pricingResult.DiscountAmount,
                TotalTax = pricingResult.TaxAmount,
                TotalAmount = pricingResult.FinalTotal,
                AmountCredited = 0,
                AmountPaid = 0,
                AmountRemaining = pricingResult.FinalTotal,
                // Taslak Statüsü (Ödeme Bekliyor)
                Status = InvoiceStatus.Open
            };
            await _unitOfWork.Repository<Invoice>().AddAsync(invoice);

            var invoiceLine = new InvoiceLine
            {
                InvoiceId = invoice.Id,
                SubscriptionId = subscription.Id,
                ProductId = product.Id,
                Description = $"{subscription.Label} - {price.Name} - Abonelik Başlangıç Ücreti",
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = pricingResult.NextBillingDate,
                Quantity = 1,
                UnitPrice = pricingResult.ProratedAmount,
                DiscountAmount = pricingResult.DiscountAmount,
                TaxRate = price.VatRate,
                TaxAmount = pricingResult.TaxAmount,
                TotalAmount = pricingResult.FinalTotal,
                IsProration = pricingResult.ProratedAmount != price.Amount
            };
            await _unitOfWork.Repository<InvoiceLine>().AddAsync(invoiceLine);
            #endregion

            #region 7. Activity Log Kaydı
            var activityLog = new SubscriptionActivityLog
            {
                SubscriptionId = subscription.Id,
                Description = $"Abonelik başlatma isteği alındı ve #{invoiceNumber} numaralı taslak fatura oluşturuldu. Nexi ödemesi bekleniyor. (Tutar: {pricingResult.FinalTotal} {price.Currency})",
                CreatedAt = DateTime.UtcNow,
                Action = "Initiate Subscription"
            };
            await _unitOfWork.Repository<SubscriptionActivityLog>().AddAsync(activityLog);
            #endregion

            // Tüm taslakları tek hamlede veritabanına mühürlüyoruz
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            #region 8. Nexi Ödeme Linki Oluşturma
            int unitPriceInCents = (int)Math.Round(pricingResult.SubTotal * 100);
            int taxAmountInCents = (int)Math.Round(pricingResult.TaxAmount * 100);
            int grossAmountInCents = (int)Math.Round(pricingResult.FinalTotal * 100);
            int netTotalAmountInCents = (int)Math.Round(pricingResult.SubTotal * 100);
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
                NetTotalAmount = netTotalAmountInCents,
                Currency = price.Currency,
                ReturnUrl = request.ReturnUrl,
                SubscriptionReference = subId
            };
            #endregion

            try
            {
                var nexiPaymentResponse = await _nexiClient.InitializePaymentAsync(nexiOrderItemDto);

                return Result<InitiateSubscriptionResponse>.Success(new InitiateSubscriptionResponse(subscription.Id, nexiPaymentResponse!.HostedPaymentPageUrl!), "Abonelik ve taslak fatura oluşturuldu, ödeme sayfası linki alındı.");
            }
            catch (Exception)
            {
                // İşlem başarısız olursa oluşturduğumuz Draft kayıtları da iptal ediyoruz (Void/Canceled)
                subscription.Status = SubscriptionStatus.Canceled;
                invoice.Status = InvoiceStatus.Void;
                invoice.FailureMessage = "Ödeme altyapısına geçici olarak ulaşılamadı, işlem iptal edildi.";
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<InitiateSubscriptionResponse>.Failure("Ödeme altyapısına geçici olarak ulaşılamıyor.");
            }
        }
        catch (Exception)
        {
            return Result<InitiateSubscriptionResponse>.Failure("İşlem sırasında beklenmeyen bir hata oluştu.");
        }
    }
}