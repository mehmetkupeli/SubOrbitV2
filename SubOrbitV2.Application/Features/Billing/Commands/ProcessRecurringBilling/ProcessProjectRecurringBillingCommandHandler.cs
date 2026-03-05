using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Application.Common.Models.Billing;
using SubOrbitV2.Application.Common.Utils;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Enums;
using SubOrbitV2.Domain.Specifications.Billing;

namespace SubOrbitV2.Application.Features.Billing.Commands.ProcessRecurringBilling;

public class ProcessProjectRecurringBillingCommandHandler : IRequestHandler<ProcessProjectRecurringBillingCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessProjectRecurringBillingCommandHandler> _logger;
    private readonly IPricingCalculatorService _pricingCalculator;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IProjectContext _projectContext;
    public ProcessProjectRecurringBillingCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ProcessProjectRecurringBillingCommandHandler> logger,
        IPricingCalculatorService pricingCalculator, IBackgroundJobClient backgroundJobClient, IProjectContext projectContext)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _pricingCalculator = pricingCalculator;
        _backgroundJobClient = backgroundJobClient;
        _projectContext = projectContext;
    }

    public async Task<Result<bool>> Handle(ProcessProjectRecurringBillingCommand request, CancellationToken cancellationToken)
    {
        if (!_projectContext.IsIdSet)
        {
            _projectContext.SetProjectId(request.ProjectId);
        }

        //var today = DateTime.UtcNow.Date;
        var today = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        _logger.LogInformation("Proje {ProjectId} için yenileme motoru başlatıldı.", request.ProjectId);

        #region Adım 1: Zeki Veri Çekimi (Payer Odaklı)

        // Sadece günü gelmiş aboneliği olan ve kayıtlı kartı bulunan Payer'ları RAM'e çekiyoruz.
        var spec = new PayersDueForRenewalSpecification(request.ProjectId, today);
        var payers = await _unitOfWork.Repository<Payer>().ListAsync(spec);

        if (!payers.Any())
        {
            _logger.LogInformation("Yenilenecek (kartı kayıtlı) müşteri bulunamadı.");
            return Result<bool>.Success(true, "İşlem gerektiren müşteri yok.");
        }

        #endregion

        var generatedInvoices = new List<Invoice>();
        var updatedSubscriptions = new List<Subscription>();

        // Her bir müşteri (Cüzdan) için döngüye giriyoruz
        foreach (var payer in payers)
        {
            // O müşterinin SADECE günü gelmiş (veya geçmiş) aktif aboneliklerini filtrele
            var dueSubscriptions = payer.Subscriptions.Where(s => s.Status == SubscriptionStatus.Active && s.NextBillingDate.Date <= today).ToList();

            if (!dueSubscriptions.Any()) continue;

            // BİRLEŞTİRİLMİŞ FATURA (Unified Invoice) TASLAĞI
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                PayerId = payer.Id,
                Number = InvoiceHelper.GenerateInvoiceNumber(),
                BillingReason = InvoiceBillingReason.SubscriptionCycle,
                CustomerName = payer.Name,
                CustomerEmail = payer.Email,
                Status = InvoiceStatus.Open, // Nexi'den çekilene kadar AÇIK
                Lines = new List<InvoiceLine>()
            };

            decimal totalSubtotal = 0;
            decimal totalTax = 0;
            decimal totalDiscount = 0;

            foreach (var sub in dueSubscriptions)
            {
                #region Adım 2: Kupon (CouponDuration) Temizlik Motoru

                if (sub.ActiveCoupon != null)
                {
                    bool removeCoupon = false;

                    if (sub.ActiveCoupon.Duration == CouponDuration.Once)
                    {
                        // Tek seferlik kuponsa, bu yenilemede (2. ay) artık geçersiz!
                        removeCoupon = true;
                    }
                    else if (sub.ActiveCoupon.Duration == CouponDuration.Repeating)
                    {
                        // Tekrarlıysa, kaç ay geçtiğini hesapla
                        int monthsPassed = ((sub.NextBillingDate.Year - sub.StartDate.Year) * 12) + sub.NextBillingDate.Month - sub.StartDate.Month;

                        // Kuponun ömrü dolduysa sök at
                        if (monthsPassed >= (sub.ActiveCoupon.DurationInMonths ?? 1))
                        {
                            removeCoupon = true;
                        }
                    }

                    if (removeCoupon)
                    {
                        sub.ActiveCouponId = null;
                        sub.ActiveCoupon = null; // Hafızadaki nesneden de koparıyoruz ki hesaplamaya yansımasın
                    }
                }

                #endregion

                #region Adım 3: Fiyatlama ve Fatura Kaleminin (InvoiceLine) Çizilmesi

                // Yenileme işlemi olduğu için proration (kıstelyevm) yok, dümdüz +1 Ay / +1 Yıl eklenecek.
                // Bu yüzden AlignmentStrategy = None gönderiyoruz.
                var pricingReq = new PricingRequest(
                    BaseAmount: sub.Price.Amount * sub.Quantity,
                    VatRate: sub.Price.VatRate,
                    Interval: sub.Price.Interval,
                    IntervalCount: sub.Price.IntervalCount,
                    AlignmentStrategy: BillingAlignmentStrategy.None,
                    AlignmentDay: 1,
                    CouponType: sub.ActiveCoupon?.DiscountType,
                    CouponValue: sub.ActiveCoupon?.DiscountValue
                );

                // Referans tarihi olarak bugünü değil, aboneliğin KENDİ NextBillingDate'ini veriyoruz.
                // (Eğer sistem 2 gün kapalı kaldıysa, aradaki fark kaybolmasın diye)
                var pricingResult = _pricingCalculator.Calculate(pricingReq, sub.NextBillingDate);

                totalSubtotal += pricingResult.SubTotal;
                totalTax += pricingResult.TaxAmount;
                totalDiscount += pricingResult.DiscountAmount;

                invoice.Lines.Add(new InvoiceLine
                {
                    InvoiceId = invoice.Id,
                    SubscriptionId = sub.Id,
                    ProductId = sub.ProductId,
                    Description = $"{sub.Label} - Periyodik Yenileme ({sub.NextBillingDate:dd.MM.yyyy} - {pricingResult.NextBillingDate:dd.MM.yyyy})",
                    PeriodStart = sub.NextBillingDate,
                    PeriodEnd = pricingResult.NextBillingDate,
                    Quantity = sub.Quantity,
                    UnitPrice = sub.Price.Amount,
                    DiscountAmount = pricingResult.DiscountAmount,
                    TaxRate = sub.Price.VatRate,
                    TaxAmount = pricingResult.TaxAmount,
                    TotalAmount = pricingResult.FinalTotal,
                    IsProration = false
                });

                // Aboneliği Geleceğe Taşı
                sub.CurrentPeriodStart = sub.NextBillingDate;
                sub.CurrentPeriodEnd = pricingResult.NextBillingDate;
                sub.NextBillingDate = pricingResult.NextBillingDate;

                updatedSubscriptions.Add(sub);

                #endregion
            }

            // Fatura Toplamlarını Mühürle
            invoice.Subtotal = totalSubtotal;
            invoice.TotalDiscount = totalDiscount;
            invoice.TotalTax = totalTax;
            invoice.TotalAmount = totalSubtotal + totalTax;
            invoice.Currency = dueSubscriptions.First().Price.Currency;

            // SIFIR TUTAR KORUMASI: Kupon sayesinde fatura 0'a indiyse Nexi'ye gitme, direkt Paid yap!
            if (invoice.TotalAmount == 0)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.AmountPaid = 0;
            }

            generatedInvoices.Add(invoice);
        }

        #region Adım 4: Chunking, Fatura Eşleştirme ve Hangfire Job Kurulumu

        // Sadece durumu 'Open' olanları Nexi'ye yollayacağız (0 Tutar olanlar zaten Paid oldu)
        var pendingInvoices = generatedInvoices.Where(x => x.Status == InvoiceStatus.Open).ToList();
        int chunkSize = 5000;
        var bulkOperations = new List<BulkOperation>();

        for (int i = 0; i < pendingInvoices.Count; i += chunkSize)
        {
            var chunk = pendingInvoices.Skip(i).Take(chunkSize).ToList();

            // Nexi Idempotency & Tracking (Takip) Kaydı
            var bulkOperation = new BulkOperation
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                Status = BulkOperationStatus.Pending,
                ItemCount = chunk.Count,
                CreatedAt = DateTime.UtcNow
            };

            bulkOperations.Add(bulkOperation);

            // KRİTİK DÜZELTME: Bu chunk'taki faturalara BulkOperationId'yi SET EDİYORUZ!
            foreach (var invoiceToUpdate in chunk)
            {
                invoiceToUpdate.BulkOperationId = bulkOperation.Id;
            }

            // 1. TODO ÇÖZÜMÜ: Hangfire Dispatcher Job'ını kuruyoruz (Örn: 0. dk, 5. dk, 10. dk...)
            var delay = JobHelper.CalculateChunkDispatchDelay(i, chunkSize, 5);
            _backgroundJobClient.Schedule<INexiBulkDispatcherJob>(
                job => job.ProcessBulkChargeAsync(request.ProjectId, bulkOperation.Id),
                delay
            );
        }

        #endregion

        #region Adım 5: Veritabanına Tek Seferde Toplu Yazım (Bulk Insert/Update)

        // Tüm atamalar (BulkOperationId dahil) hafızada yapıldı, şimdi tek seferde DB'ye yazıyoruz!
        if (generatedInvoices.Any())
        {
            await _unitOfWork.Repository<Invoice>().AddRangeAsync(generatedInvoices);
            _unitOfWork.Repository<Subscription>().UpdateRange(updatedSubscriptions);

            if (bulkOperations.Any())
            {
                await _unitOfWork.Repository<BulkOperation>().AddRangeAsync(bulkOperations);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        #endregion

        _logger.LogInformation("Yenileme tamamlandı: {InvoiceCount} fatura kesildi, {SubCount} abonelik güncellendi. {BulkCount} adet Bulk işlem kuyruğa alındı.",
            generatedInvoices.Count, updatedSubscriptions.Count, bulkOperations.Count);

        return Result<bool>.Success(true);
    }
}