using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Organization;

namespace SubOrbitV2.Application.Common.Interfaces;

/// <summary>
/// Sistemdeki bildirimleri (E-posta, SMS) hazırlayıp kuyruğa (Outbox) alan yönetici servis.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Fatura kesildiğinde müşteriye şablonlu bir e-posta bildirimini kuyruğa atar.
    /// </summary>
    Task NotifyInvoiceCreatedAsync(Guid projectId, Payer payer, Invoice invoice, Project project, string pdfPath);
}