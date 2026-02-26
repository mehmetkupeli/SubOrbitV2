using SubOrbitV2.Domain.Abstractions;

namespace SubOrbitV2.Domain.Entities.Billing;

/// <summary>
/// Abonelik üzerinde yapılan işlemlerin tarihçesi (Timeline).
/// Kullanıcı panelinde "Geçmiş İşlemler" olarak gösterilir.
/// </summary>
public class SubscriptionActivityLog : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// İşlem Tipi: "Upgrade", "Downgrade", "Renewal", "PaymentFailed".
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Kullanıcı dostu açıklama.
    /// Örn: "Paket Gold seviyesine yükseltildi."
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// İşlemi kim yaptı? (Sistem, Admin, Kullanıcı).
    /// </summary>
    public string PerformedBy { get; set; } = "System";

    /// <summary>
    /// İşlem anındaki IP adresi (Varsa).
    /// </summary>
    public string? IpAddress { get; set; }
}