using System.Text.Json.Serialization;

namespace SubOrbitV2.Application.Common.Models.Webhooks;

#region Main Payload Envelope
/// <summary>
/// Müşteri sistemlerine (Tenant'lara) gönderilecek standart ve tip güvenli Webhook zarfı.
/// </summary>
public class AccessWebhookPayload
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty; // access.granted, access.revoked vb.

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("user")]
    public WebhookUser User { get; set; } = new();

    [JsonPropertyName("access")]
    public WebhookAccessDetails Access { get; set; } = new();
}
#endregion

#region Payload Details
public class WebhookUser
{
    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; } = string.Empty; // Müşterinin kendi sistemindeki ID'si

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public class WebhookAccessDetails
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Active";

    [JsonPropertyName("planName")]
    public string PlanName { get; set; } = string.Empty;

    [JsonPropertyName("planId")]
    public string PlanId { get; set; } = string.Empty;

    [JsonPropertyName("validUntil")]
    public DateTime? ValidUntil { get; set; }

    // Müşterinin, "Bu kullanıcının yetkileri neler?" sorusunun cevabı.
    [JsonPropertyName("features")]
    public List<WebhookFeatureDto> Features { get; set; } = new();
}

public class WebhookFeatureDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty; // Kod tarafındaki karşılığı (Örn: invoice_create)

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty; // Okunabilir adı

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty; // Özelliğin limiti veya değeri (Örn: true, 10, Sınırsız)
}
#endregion