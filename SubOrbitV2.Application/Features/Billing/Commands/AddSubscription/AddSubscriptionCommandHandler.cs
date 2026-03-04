using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Application.Common.Models.Billing;
using SubOrbitV2.Application.Common.Models.Payment;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Entities.Organization;
using SubOrbitV2.Domain.Enums;
using SubOrbitV2.Domain.Specifications.Billing;
using SubOrbitV2.Domain.Specifications.Catalog;

namespace SubOrbitV2.Application.Features.Billing.Commands.AddSubscription;

public class AddSubscriptionCommandHandler : IRequestHandler<AddSubscriptionCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectContext _projectContext;
    private readonly IPricingCalculatorService _pricingCalculator;
    private readonly INexiClient _nexiClient;
    private readonly IPdfService _pdfService;
    private readonly IFileService _fileService;
    private readonly IWebhookService _webhookService;
    private readonly INotificationService _notificationService;

    public AddSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        IProjectContext projectContext,
        IPricingCalculatorService pricingCalculator,
        INexiClient nexiClient,
        IPdfService pdfService,
        IFileService fileService,
        IWebhookService webhookService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _projectContext = projectContext;
        _pricingCalculator = pricingCalculator;
        _nexiClient = nexiClient;
        _pdfService = pdfService;
        _fileService = fileService;
        _webhookService = webhookService;
        _notificationService = notificationService;
    }

    public async Task<Result<bool>> Handle(AddSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var projectId = _projectContext.ProjectId;

        #region Aşama 1: Validasyon ve Güvenlik Duvarı

        // 1. Projeyi çek
        var project = await _unitOfWork.Repository<Project>().GetByIdAsync(projectId);
        if (project == null) return Result<bool>.Failure("Proje bulunamadı.");

        // 2. Payer (Ödeyen Ana Müşteri) Kontrolü
        var payerSpec = new PayerWithSubscriptionsByExternalIdSpecification(projectId, request.PayerExternalId);
        var payer = await _unitOfWork.Repository<Payer>().GetEntityWithSpec(payerSpec);

        if (payer == null || payer.Status != PayerStatus.Active)
            return Result<bool>.Failure("Ödemeyi yapacak ana müşteri bulunamadı veya hesabı aktif değil.");

        if (string.IsNullOrEmpty(payer.NexiCustomerId))
            return Result<bool>.Failure("Müşterinin kayıtlı bir kartı bulunmuyor. Otomatik tahsilat yapılamaz.");

        // 3. Mükerrerlik Kontrolü
        if (payer.Subscriptions.Any(s => s.ExternalId == request.SubscriptionExternalId && s.Status == SubscriptionStatus.Active))
            return Result<bool>.Failure($"'{request.SubscriptionExternalId}' ID'li kullanıcı için zaten aktif bir paket mevcut.");

        // 4. Ürün, Fiyat ve Özellikleri Yükleme (Webhook için özellikler şart)
        var price = await _unitOfWork.Repository<Price>().GetByIdAsync(request.PriceId);
        if (price == null || price.ProjectId != projectId || !price.IsActive)
            return Result<bool>.Failure("Geçersiz veya pasif paket seçimi.");

        var productSpec = new ProductWithFeaturesSpecification(projectId, price.ProductId);
        var product = await _unitOfWork.Repository<Product>().GetEntityWithSpec(productSpec);

        // 5. Kupon Kontrolü
        Coupon? coupon = null;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var couponSpec = new CouponByCodeSpecification(projectId, request.CouponCode);
            coupon = await _unitOfWork.Repository<Coupon>().GetEntityWithSpec(couponSpec);

            if (coupon == null || !coupon.IsActive || (coupon.ExpiryDate.HasValue && coupon.ExpiryDate < DateTime.UtcNow))
                return Result<bool>.Failure("Geçersiz veya süresi dolmuş kupon.");
        }
        #endregion

        #region Aşama 2: Kıstelyevm (Proration) Hesabı
        var pricingRequest = new PricingRequest(
            BaseAmount: price.Amount * request.Quantity,
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

        #region Aşama 3: Hafızada (Memory) Kayıtları Hazırlama

        var subscriptionId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";

        var subscription = new Subscription
        {
            Id = subscriptionId,
            PayerId = payer.Id,
            ProductId = product!.Id,
            PriceId = price.Id,
            ExternalId = request.SubscriptionExternalId,
            Label = request.Label ?? price.Name,
            IsMain = false, 
            Status = SubscriptionStatus.Active,
            CurrentPeriodStart = DateTime.UtcNow,
            NextBillingDate = pricingResult.NextBillingDate,
            CurrentPeriodEnd = pricingResult.NextBillingDate,
            Quantity = request.Quantity,
            ActiveCouponId = coupon?.Id
        };

        var invoice = new Invoice
        {
            Id = invoiceId,
            ProjectId = projectId,
            PayerId = payer.Id,
            Number = invoiceNumber,
            BillingReason = InvoiceBillingReason.SubscriptionUpdate, 
            CustomerName = payer.Name,
            CustomerEmail = payer.Email,
            CustomerTaxOffice = payer.TaxOffice,
            CustomerTaxNumber = payer.TaxNumber,
            CustomerAddress = payer.BillingAddress,
            CustomerCity = payer.City,
            CustomerCountry = payer.Country,
            PeriodStart = DateTime.UtcNow,
            PeriodEnd = pricingResult.NextBillingDate,
            DueDate = DateTime.UtcNow,
            Currency = price.Currency,
            Subtotal = pricingResult.ProratedAmount,
            TotalDiscount = pricingResult.DiscountAmount,
            TotalTax = pricingResult.TaxAmount,
            TotalAmount = pricingResult.FinalTotal,
            AmountPaid = pricingResult.FinalTotal, 
            AmountRemaining = 0,
            Status = InvoiceStatus.Paid, 
            PaidAt = DateTime.UtcNow
        };

        var invoiceLine = new InvoiceLine
        {
            InvoiceId = invoice.Id,
            SubscriptionId = subscription.Id,
            ProductId = product.Id,
            Description = $"{subscription.Label} - Eklenti Başlangıç Ücreti",
            PeriodStart = DateTime.UtcNow,
            PeriodEnd = pricingResult.NextBillingDate,
            Quantity = request.Quantity,
            UnitPrice = pricingResult.ProratedAmount,
            DiscountAmount = pricingResult.DiscountAmount,
            TaxRate = price.VatRate,
            TaxAmount = pricingResult.TaxAmount,
            TotalAmount = pricingResult.FinalTotal,
            IsProration = pricingResult.ProratedAmount != (price.Amount * request.Quantity)
        };

        // Bu nesneleri Entity Framework'ün izlemesine (Tracking) alıyoruz ama VERİTABANINA YAZMIYORUZ.
        await _unitOfWork.Repository<Subscription>().AddAsync(subscription);
        await _unitOfWork.Repository<Invoice>().AddAsync(invoice);
        await _unitOfWork.Repository<InvoiceLine>().AddAsync(invoiceLine);

        #endregion

        #region Aşama 4: Nexi'den Anında Tahsilat (Auto-Charge)

        // 0 Tutar kontrolü (Eğer kupon %100 veya deneme sürümü ise Nexi'ye gitmeye gerek yok)
        bool paymentSuccess = true;
        string? nexiChargeId = null;

        if (pricingResult.FinalTotal > 0)
        {
            var nexiOrderItem = new NexiOrderItem(
                Reference: price.Id.ToString(),
                Name: subscription.Label,
                Quantity: request.Quantity,
                Unit: "pcs",
                UnitPrice: (int)Math.Round(pricingResult.SubTotal * 100),
                TaxRate: (int)Math.Round(price.VatRate * 100),
                TaxAmount: (int)Math.Round(pricingResult.TaxAmount * 100),
                GrossTotalAmount: (int)Math.Round(pricingResult.FinalTotal * 100),
                NetTotalAmount: (int)Math.Round(pricingResult.SubTotal * 100)
            );

            // Nexi API Vurşu (Senkron)
            var chargeResult = await _nexiClient.ChargeSubscriptionAsync(payer.NexiCustomerId, nexiOrderItem, price.Currency, invoice.Id);

            paymentSuccess = chargeResult.Success;
            nexiChargeId = chargeResult.ChargeId;
        }

        // Ödeme başarısızsa, hazırladığımız her şeyi çöpe at ve anında dön!
        if (!paymentSuccess)
        {
            return Result<bool>.Failure("Müşterinin kayıtlı kartından tahsilat yapılamadı (Örn: Limit yetersiz). İşlem iptal edildi.");
        }

        invoice.NexiTransactionId = nexiChargeId;
        #endregion

        #region Aşama 5: Fulfillment ve Sonlandırma

        // 1. Cüzdan, Log, CouponRedemption Kayıtları
        var walletTransaction = new WalletTransaction
        {
            ProjectId = projectId,
            PayerId = payer.Id,
            SubscriptionItemId = subscription.Id,
            Amount = pricingResult.FinalTotal,
            Currency = price.Currency,
            Type = WalletTransactionType.Payment, 
            Description = $"Eklenti tahsilatı yapıldı. Fatura: #{invoiceNumber}",
            BalanceAfter = payer.VirtualBalance
        };
        await _unitOfWork.Repository<WalletTransaction>().AddAsync(walletTransaction);

        var activityLog = new SubscriptionActivityLog
        {
            SubscriptionId = subscription.Id,
            Action = "Add-On Created",
            Description = $"Arka planda {pricingResult.FinalTotal} {price.Currency} tahsil edilerek yeni eklenti aktifleştirildi.",
            PerformedBy = "System"
        };
        await _unitOfWork.Repository<SubscriptionActivityLog>().AddAsync(activityLog);

        
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

        // 2. Her Şeyi Veritabanına Mühürle (Transaction Commit)
        // Artık paramızı aldık, DB'ye güvenle yazabiliriz.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Fatura PDF Üretimi
        // Not: Faturaya "Satırları" manuel ekliyoruz ki PDF çizerken okuyabilsin (Az önce save attık ama Include yapmadık)
        invoice.Lines = new List<InvoiceLine> { invoiceLine };
        var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(invoice, project);
        var pdfPath = await _fileService.SaveFileAsync(pdfBytes, $"{invoice.Number}.pdf", "invoices", payer.Id.ToString());

        invoice.PdfPath = pdfPath;
        _unitOfWork.Repository<Invoice>().Update(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken); 

        // 4. Webhook ve E-Posta Dağıtımları
        // DİKKAT: Webhook'a Payer nesnesini veriyoruz ama içine ALT KULLANICI'nın ID'sini eziyoruz ki Tenant kimin yetkileneceğini bilsin.
        var tempPayerForWebhook = new Payer { ExternalId = request.SubscriptionExternalId, Email = payer.Email };

        var webhookEventId = await _webhookService.NotifyAccessGrantedAsync(projectId, tempPayerForWebhook, product, subscription.NextBillingDate, product.FeatureValues.ToList());

        // Kısmi ödeme uyarısını e-postaya basalım mı?
        bool isProrated = invoiceLine.IsProration;

        var notificationId = await _notificationService.NotifyWelcomeAndSubscriptionAsync(
            projectId, payer, invoice, project, pdfPath, isProrated
        );

        // 5. Arka plan işçilerini tetikle
        _webhookService.DispatchBackgroundJob(webhookEventId);
        _notificationService.DispatchBackgroundJob(notificationId);

        #endregion

        return Result<bool>.Success(true, "Paket başarıyla eklendi ve tahsilat anında gerçekleştirildi.");
    }
}