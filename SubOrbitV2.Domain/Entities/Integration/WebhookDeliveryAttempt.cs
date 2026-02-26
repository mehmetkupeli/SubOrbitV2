using SubOrbitV2.Domain.Abstractions;

namespace SubOrbitV2.Domain.Entities.Integration;

/// <summary>
/// Webhook olayının karşı sunucuya iletilme denemeleri.
/// </summary>
public class WebhookDeliveryAttempt : BaseEntity
{
    public Guid WebhookEventId { get; set; }

    /// <summary>
    /// İstek atılan URL.
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// HTTP Status Code (200, 404, 500).
    /// </summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>
    /// Karşı sunucudan dönen cevap (Hata detayı vb.).
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// İşlem süresi (ms).
    /// </summary>
    public long DurationMs { get; set; }

    public bool IsSuccess => ResponseStatusCode >= 200 && ResponseStatusCode < 300;
}