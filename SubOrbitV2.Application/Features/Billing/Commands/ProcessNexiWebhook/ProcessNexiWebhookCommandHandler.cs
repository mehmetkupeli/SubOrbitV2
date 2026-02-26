using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Entities.Integration;
using SubOrbitV2.Domain.Entities.Organization;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Application.Features.Billing.Commands.ProcessNexiWebhook;

public class ProcessNexiWebhookCommandHandler : IRequestHandler<ProcessNexiWebhookCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;

    public ProcessNexiWebhookCommandHandler(IUnitOfWork unitOfWork, IEncryptionService encryptionService)
    {
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
    }

    public async Task<Result<bool>> Handle(ProcessNexiWebhookCommand request, CancellationToken cancellationToken)
    {
        #region 1. Güvenlik ve Doğrulama
        var project = await _unitOfWork.Repository<Project>().GetByIdAsync(request.ProjectId);
        if (project == null) return Result<bool>.Failure("Proje bulunamadı.");

        #endregion

        #region 2. Payload Analizi (Subscription ID)
        if (string.IsNullOrWhiteSpace(request.SubId) || !request.SubId.StartsWith("SUB-"))
            return Result<bool>.Failure("Geçersiz referans formatı.");

        if (!Guid.TryParse(request.SubId.Replace("SUB-", ""), out var subscriptionId))
            return Result<bool>.Failure("Geçersiz Subscription Guid formatı.");
        #endregion

        #region 3. Idempotency (Mükerrer İstek Koruması)
        var subscription = await _unitOfWork.Repository<Subscription>().GetByIdAsync(subscriptionId);
        if (subscription == null || subscription.ProjectId != request.ProjectId)
            return Result<bool>.Failure("Abonelik taslağı bulunamadı.");

        if (subscription.Status == SubscriptionStatus.Active)
        {
            return Result<bool>.Success(true, "Bu abonelik zaten aktifleştirilmiş.");
        }
        #endregion

        #region 4. Gerekli Entity'leri Toplama
        var payer = await _unitOfWork.Repository<Payer>().GetByIdAsync(subscription.PayerId);
        var price = await _unitOfWork.Repository<Price>().GetByIdAsync(subscription.PriceId);

        if (payer == null || price == null)
            return Result<bool>.Failure("Aboneliğe bağlı Payer veya Price bulunamadı.");
        #endregion

        try
        {
            #region 5. BÜYÜK PATLAMA (Unit of Work İşlemleri)

            // İşlem 1: Abonelik Aktifleştirme
            subscription.Status = SubscriptionStatus.Active;
            _unitOfWork.Repository<Subscription>().Update(subscription);

            // İşlem 2: Müşteri (Payer) Aktifleştirme
            payer.Status = PayerStatus.Active;
            _unitOfWork.Repository<Payer>().Update(payer);

            // İşlem 3: Fatura (Invoice) Oluşturma
            var invoice = new Invoice
            {
                ProjectId = request.ProjectId,
                PayerId = payer.Id,
                SubscriptionId = subscription.Id,
                Amount = price.Amount, // Gerçek senaryoda indirimli net tutarı buraya atacağız
                Currency = price.Currency,
                Status = InvoiceStatus.Paid, // Nexi'den para geldiği için anında Paid
                BillingReason = InvoiceBillingReason.SubscriptionCreate,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow,
                PaidAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<Invoice>().AddAsync(invoice);

            // İşlem 4: Fatura Kalemi (InvoiceLine)
            var invoiceLine = new InvoiceLine
            {
                InvoiceId = invoice.Id,
                Description = $"{price.Name} - Abonelik Başlangıç Ücreti",
                Amount = price.Amount,
                Quantity = 1
            };
            await _unitOfWork.Repository<InvoiceLine>().AddAsync(invoiceLine);

            // İşlem 5: Cüzdan Hareketi (WalletTransaction) - Kasaya Para Girdi!
            var walletTransaction = new WalletTransaction
            {
                ProjectId = request.ProjectId,
                PayerId = payer.Id,
                Amount = price.Amount,
                Currency = price.Currency,
                Type = WalletTransactionType.Deposit,
                Description = $"Nexi ödemesi alındı. (Ref: {request.SubId})",
            };
            await _unitOfWork.Repository<WalletTransaction>().AddAsync(walletTransaction);

            // İşlem 6: Aktivite Logu
            var activityLog = new SubscriptionActivityLog
            {
                SubscriptionId = subscription.Id,
                Action = "Payment.Success",
                Description = "Nexi üzerinden ödeme başarıyla alındı. Abonelik ve fatura işlemleri tamamlandı.",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<SubscriptionActivityLog>().AddAsync(activityLog);

            // İşlem 7: Outbound Webhook (X Muhasebe'ye Haber Verme - Faz 4 İçin)
            // Bu tabloya kaydı atıyoruz, Hangfire veya Background Service arkada bunu görüp X Muhasebe'ye fırlatacak.
            var webhookEvent = new WebhookEvent
            {
                ProjectId = request.ProjectId,
                EventType = "subscription.activated",
                Payload = $"{{\"ExternalId\": \"{payer.ExternalId}\", \"SubscriptionId\": \"{subscription.Id}\"}}",
                Status = WebhookEventStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<WebhookEvent>().AddAsync(webhookEvent);

            // TÜM SİSTEMİ TEK SEFERDE MÜHÜRLE
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            #endregion

            return Result<bool>.Success(true, "Tüm Fulfillment işlemleri başarıyla tamamlandı.");
        }
        catch (Exception)
        {
            // DB Transaction hata verirse EF Core otomatik Rollback yapar, 
            // Biz sadece Nexi'ye 500 hatası dönmek yerine mantıklı bir false dönüyoruz.
            return Result<bool>.Failure("Webhook işlenirken sistem içi bir hata oluştu.");
        }
    }
}