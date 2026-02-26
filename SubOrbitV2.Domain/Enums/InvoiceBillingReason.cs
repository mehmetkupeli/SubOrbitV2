namespace SubOrbitV2.Domain.Enums;

public enum InvoiceBillingReason
{
    /// <summary>
    /// Standart dönemsel yenileme faturası (Ayın 1'i).
    /// </summary>
    SubscriptionCycle = 1,

    /// <summary>
    /// Paket değişikliği (Upgrade) kaynaklı anlık fatura.
    /// </summary>
    SubscriptionUpdate = 2,

    /// <summary>
    /// Admin tarafından manuel oluşturulan fatura.
    /// </summary>
    Manual = 3
}
