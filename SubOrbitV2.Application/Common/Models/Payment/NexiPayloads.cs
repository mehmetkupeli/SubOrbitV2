using System.Text.Json.Serialization;

namespace SubOrbitV2.Application.Common.Models.Payment;


#region Request Models

public record CreatePaymentRequest(
    [property: JsonPropertyName("checkout")] NexiCheckout Checkout,
    [property: JsonPropertyName("order")] NexiOrder Order,
    [property: JsonPropertyName("notifications")] NexiNotification Notifications,
    [property: JsonPropertyName("subscription")] NexiSubscriptionRequestData Subscription
);

public record NexiCheckout(
    [property: JsonPropertyName("integrationType")] string IntegrationType,
    [property: JsonPropertyName("returnUrl")] string ReturnUrl,
    [property: JsonPropertyName("termsUrl")] string TermsUrl,
    [property: JsonPropertyName("charge")] bool Charge,
    [property: JsonPropertyName("merchantHandlesConsumerData")] bool MerchantHandlesConsumerData
);

public record NexiSubscriptionRequestData(
    [property: JsonPropertyName("subscriptionId")] string? SubscriptionId = null,
    [property: JsonPropertyName("interval")] int Interval = default,
    [property: JsonPropertyName("endDate")] string? EndDate = null
);

public record NexiOrder(
    [property: JsonPropertyName("items")] IEnumerable<NexiOrderItem> Items,
    [property: JsonPropertyName("amount")] int Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("reference")] string Reference
);

public record NexiOrderItem(
    [property: JsonPropertyName("reference")] string Reference,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("unit")] string Unit,
    [property: JsonPropertyName("unitPrice")] int UnitPrice,
    [property: JsonPropertyName("taxRate")] int TaxRate,
    [property: JsonPropertyName("taxAmount")] int TaxAmount,
    [property: JsonPropertyName("grossTotalAmount")] int GrossTotalAmount,
    [property: JsonPropertyName("netTotalAmount")] int NetTotalAmount
);

public record NexiNotification(
    [property: JsonPropertyName("webHooks")] IEnumerable<NexiWebhook> WebHooks
);

public record NexiWebhook(
    [property: JsonPropertyName("eventName")] string EventName,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("authorization")] string Authorization
);

#endregion

#region Response Models

public record NexiPaymentResponse(
    [property: JsonPropertyName("paymentId")] string? PaymentId,
    [property: JsonPropertyName("hostedPaymentPageUrl")] string? HostedPaymentPageUrl
);
public record NexiPaymentDetailsResponse(
    [property: JsonPropertyName("payment")] NexiPaymentDetailsInner Payment
);

public record NexiPaymentDetailsInner(
    [property: JsonPropertyName("paymentId")] string PaymentId,
    [property: JsonPropertyName("subscription")] NexiSubscriptionDetails? Subscription,
    [property: JsonPropertyName("summary")] NexiPaymentSummary? Summary
);

public record NexiSubscriptionDetails(
    [property: JsonPropertyName("id")] string Id
);

public record NexiPaymentSummary(
    [property: JsonPropertyName("reservedAmount")] int ReservedAmount,
    [property: JsonPropertyName("chargedAmount")] int ChargedAmount
);
public class NexiWebhookPayload
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("merchantId")]
    public long MerchantId { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("data")]
    public NexiWebhookData Data { get; set; } = new();
}

public class NexiWebhookData
{
    [JsonPropertyName("paymentId")]
    public string PaymentId { get; set; } = string.Empty;
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("bulkId")]
    public string BulkId { get; set; } = string.Empty;
}
#endregion