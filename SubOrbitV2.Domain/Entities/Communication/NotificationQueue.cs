using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Entities.Communication;

/// <summary>
/// Sistemden dışarıya gidecek bildirimlerin kuyruğu.
/// Email, SMS vb. dinamik kanalları destekler.
/// </summary>
public class NotificationQueue : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Alıcı adresi (Email, Telefon No vb.).
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Bildirim kanalı (Email, SMS).
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Konu başlığı (Email Subject).
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// İçerik (HTML veya Text).
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gönderim durumu.
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>
    /// Hata mesajı (SMTP hatası vb.).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Kaç kez denendi?
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Ne zaman gönderilmeli? (İleri tarihli gönderim için).
    /// </summary>
    public DateTime ScheduledTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gönderildiği an.
    /// </summary>
    public DateTime? SentAt { get; set; }
}