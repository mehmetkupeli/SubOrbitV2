using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Entities.Billing;

/// <summary>
/// Müşteri bakiyesindeki (Payer veya SubscriptionItem) tüm hareketlerin tarihçesi.
/// Muhasebeleşme ve raporlama için kullanılır.
/// </summary>
public class WalletTransaction : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    /// <summary>
    /// İşlem hangi ana cüzdana (Payer) ait?
    /// </summary>
    public Guid PayerId { get; set; }

    /// <summary>
    /// İşlem spesifik bir alt aboneliğe (Item) mi ait?
    /// Null ise Payer'ın ana cüzdan hareketidir.
    /// </summary>
    public Guid? SubscriptionItemId { get; set; }

    /// <summary>
    /// İşlem tutarı.
    /// Pozitif (+) değerler bakiye artırır (Deposit, Refund).
    /// Negatif (-) değerler bakiye azaltır (Payment).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// İşlem anındaki para birimi.
    /// </summary>
    public string Currency { get; set; } = "DKK";

    /// <summary>
    /// İşlemin türü (Ödeme, İade, Transfer vb.).
    /// </summary>
    public WalletTransactionType Type { get; set; }

    /// <summary>
    /// İşlem açıklaması.
    /// Örn: "Muhasebe Paketi İptal İadesi" veya "Ocak 2026 Fatura Ödemesi".
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// İşlem sonrası oluşan güncel bakiye (Snapshot).
    /// Denetim (Audit) kolaylığı sağlar.
    /// </summary>
    public decimal BalanceAfter { get; set; }

    // Navigation
    public virtual Payer Payer { get; set; }
    public virtual Subscription? Subscription { get; set; }
}