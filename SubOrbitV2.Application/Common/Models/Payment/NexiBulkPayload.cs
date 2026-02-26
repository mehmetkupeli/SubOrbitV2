using System.Text.Json.Serialization;

namespace SubOrbitV2.Application.Common.Models.Payment;

public record BulkChargeRequest(
    [property: JsonPropertyName("externalBulkChargeId")] string ExternalBulkChargeId, // Bizim tarafımızdaki Batch ID (Opsiyonel)
    [property: JsonPropertyName("notifications")] NexiNotification? Notifications, // Webhook için
    [property: JsonPropertyName("subscriptions")] IEnumerable<BulkSubscriptionItem> Subscriptions
);

public record BulkSubscriptionItem(
    [property: JsonPropertyName("subscriptionId")] string SubscriptionId, // Nexi'deki Sub ID (Token)
    [property: JsonPropertyName("order")] NexiOrder Order // Tutar ve Para Birimi
);

// --- RESPONSE (Başlatma) ---
public record BulkChargeResponse(
    [property: JsonPropertyName("bulkId")] string BulkId // Takip numaramız
);

// --- RESPONSE (Sonuç Sorgulama) ---
public record BulkStatusResponse(
    [property: JsonPropertyName("bulkId")] string BulkId,
    [property: JsonPropertyName("status")] string Status, // "Done", "Processing"
    [property: JsonPropertyName("page")] IEnumerable<BulkItemResult> Pages,
    [property: JsonPropertyName("more")] bool More
);

public record BulkItemResult(
    [property: JsonPropertyName("subscriptionId")] string SubscriptionId,
    [property: JsonPropertyName("chargeId")] string? ChargeId, // Başarılıysa dolu
    [property: JsonPropertyName("status")] string Status, // "Succeeded", "Failed"
    [property: JsonPropertyName("error")] dynamic? Error // Hata detayı
);