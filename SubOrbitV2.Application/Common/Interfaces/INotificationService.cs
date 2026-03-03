using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Organization;

namespace SubOrbitV2.Application.Common.Interfaces;

/// <summary>
/// Sistemdeki bildirimleri (E-posta, SMS) hazırlayıp kuyruğa (Outbox) alan yönetici servis.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Yeni abone olan müşteriye "Hoş Geldiniz" mesajı ve ilk faturasını şablonlu bir şekilde hazırlar.
    /// (Sadece abonelik ilk başladığında kullanılır)
    /// </summary>
    Task<Guid> NotifyWelcomeAndSubscriptionAsync(Guid projectId, Payer payer, Invoice invoice, Project project, string pdfPath, bool isProrated = false);

    /// <summary>
    /// Mevcut abonelere aylık yenilemelerde (Recurring) gönderilen standart fatura bildirimini hazırlar.
    /// </summary>
    Task<Guid> NotifyStandardInvoiceAsync(Guid projectId, Payer payer, Invoice invoice, Project project, string pdfPath);

    /// <summary>
    /// Hazırlanıp veritabanına eklenen bildirimi Hangfire kuyruğuna fırlatır.
    /// </summary>
    void DispatchBackgroundJob(Guid notificationId);
}