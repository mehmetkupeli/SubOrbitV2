using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Integration;
using SubOrbitV2.Domain.Entities.Organization;
using SubOrbitV2.Domain.Enums;
using SubOrbitV2.Domain.Specifications.Billing;

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

        #region 4. Gerekli Entity'leri Toplama (Faturayı Yakalama)
        var payer = await _unitOfWork.Repository<Payer>().GetByIdAsync(subscription.PayerId);

        // Yeni Specification'ı kullanarak bu aboneliğe ait açık faturayı buluyoruz.
        var invoiceSpec = new InvoiceBySubscriptionIdSpecification(request.ProjectId, subscription.Id);
        var invoice = await _unitOfWork.Repository<Invoice>().GetEntityWithSpec(invoiceSpec);

        if (payer == null || invoice == null)
            return Result<bool>.Failure("Aboneliğe bağlı Payer veya açık durumdaki Taslak Fatura bulunamadı.");
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

            // İşlem 3: Mevcut Taslak Faturayı (Invoice) Tahsil Edildi Olarak İşaretleme
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
            invoice.AmountPaid = invoice.TotalAmount;
            invoice.AmountRemaining = 0;
            invoice.NexiTransactionId = request.SubId; // Referans kodunu setliyoruz
            _unitOfWork.Repository<Invoice>().Update(invoice);


            // İşlem 4: Cüzdan Hareketi (WalletTransaction) - Kasaya Para Girdi!
            // Faturadaki tahsil edilen net tutarı (TotalAmount) kullanıyoruz
            var walletTransaction = new WalletTransaction
            {
                ProjectId = request.ProjectId,
                PayerId = payer.Id,
                SubscriptionItemId = subscription.Id, // Hangi alt abonelikten geldiğini de bilelim
                Amount = invoice.TotalAmount,
                Currency = invoice.Currency,
                Type = WalletTransactionType.Deposit,
                Description = $"Nexi ödemesi alındı. Fatura: #{invoice.Number} (Ref: {request.SubId})",
                BalanceAfter = payer.VirtualBalance // İlk işlem olduğu için bakiye değişimi sıfıra-sıfır, bu alan audit içindir.
            };
            await _unitOfWork.Repository<WalletTransaction>().AddAsync(walletTransaction);

            // İşlem 5: Aktivite Logu
            var activityLog = new SubscriptionActivityLog
            {
                SubscriptionId = subscription.Id,
                Action = "Payment.Success",
                Description = $"Nexi üzerinden ödeme başarıyla alındı. #{invoice.Number} numaralı fatura 'Ödendi' olarak işaretlendi.",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<SubscriptionActivityLog>().AddAsync(activityLog);

            // İşlem 6: Outbound Webhook (Müşteriye/Muhasebeye Haber Verme)
            var webhookEvent = new WebhookEvent
            {
                ProjectId = request.ProjectId,
                EventType = "subscription.activated",
                Payload = $"{{\"ExternalId\": \"{payer.ExternalId}\", \"SubscriptionId\": \"{subscription.Id}\", \"InvoiceId\": \"{invoice.Id}\"}}",
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
            return Result<bool>.Failure("Webhook işlenirken sistem içi bir hata oluştu.");
        }
    }
}