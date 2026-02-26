using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Entities.Integration;

public class WebhookEvent : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Olay Tipi (Örn: "invoice.paid", "subscription.created").
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gönderilecek veri (JSON).
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// İlgili kaynağın ID'si (InvoiceId vb.).
    /// </summary>
    public Guid ResourceId { get; set; }

    // --- Retry ve Status Mekanizması ---

    /// <summary>
    /// Webhook gönderim durumu.
    /// </summary>
    public WebhookEventStatus Status { get; set; } = WebhookEventStatus.Pending;

    /// <summary>
    /// Kaç kez denendi?
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Bir sonraki deneme ne zaman yapılmalı?
    /// Null ise ve statü Pending/Retrying ise hemen denenmeli.
    /// Exponential Backoff (üstel bekleme) için kullanılır.
    /// </summary>
    public DateTime? NextRetryDate { get; set; }

    /// <summary>
    /// Hata durumunda son alınan hata mesajı.
    /// </summary>
    public string? LastError { get; set; }
}