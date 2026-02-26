using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Entities.Billing;

/// <summary>
/// Sistemin finansal muhatabı olan Cüzdan/Müşteri varlığı.
/// Unified Billing yapısında 'Ana Sözleşme' görevi görür.
/// Altındaki tüm SubscriptionItem'ların ödemesi buradan yönetilir.
/// </summary>
public class Payer : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    #region Kimlik ve İletişim (Identity)

    /// <summary>
    /// Müşterinin kaynak sistemdeki benzersiz ID'si (Örn: "COMP-101").
    /// (Index: ProjectId + ExternalId -> Unique).
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Fatura Ünvanı veya Ad Soyad.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Fatura ve bildirim e-postası.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    #endregion

    #region Fatura Detayları (Tax & Address)

    public string? TaxOffice { get; set; }

    /// <summary>
    /// Vergi Numarası veya TC Kimlik No.
    /// </summary>
    public string? TaxNumber { get; set; }

    public string? BillingAddress { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }

    #endregion

    #region Ödeme ve Bakiye (Payment & Balance)

    /// <summary>
    /// Cüzdanın para birimi. Varsayılan: DKK.
    /// Tüm tahsilatlar bu para birimi üzerinden yapılır.
    /// </summary>
    public string Currency { get; set; } = "DKK";

    /// <summary>
    /// Nexi API tarafından verilen müşteri token'ı.
    /// Null ise kayıtlı kartı yok demektir.
    /// </summary>
    public string? NexiCustomerId { get; set; }

    /// <summary>
    /// Müşterinin sistemdeki alacak bakiyesi.
    /// İptal veya paket düşürme (Downgrade) işlemlerinden artan tutar buraya eklenir.
    /// Fatura kesilirken önce buradaki tutar kullanılır.
    /// </summary>
    public decimal VirtualBalance { get; set; } = 0;

    #endregion

    #region Hizalama Motoru (Alignment Engine)

    /// <summary>
    /// Fatura kesim günü hizalama tercihi.
    /// </summary>
    public BillingAlignmentStrategy AlignmentStrategy { get; set; } = BillingAlignmentStrategy.None;

    /// <summary>
    /// Çapa Günü (Anchor Day).
    /// Müşterinin faturasının her ayın kaçında kesileceğini belirler (1-31).
    /// Strategy=Quarter ise genelde 1 olarak set edilir.
    /// </summary>
    public int BillingAnchorDay { get; set; }

    /// <summary>
    /// Bir sonraki 'Ana Fatura' kesim tarihi.
    /// Sistem bu tarihe göre SubscriptionItem'ları hizalar.
    /// Örn: Quarter seçildiyse bir sonraki 1 Nisan tarihi burada tutulur.
    /// </summary>
    public DateTime? BillingCycleAnchor { get; set; }

    #endregion

    #region Durum ve Kurtarma (Status & Dunning)

    public PayerStatus Status { get; set; } = PayerStatus.Active;

    /// <summary>
    /// Son ödeme başarısız olduğunda artan sayaç.
    /// Başarılı ödemede 0'a çekilir.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Bir sonraki ödeme denemesinin yapılacağı tarih.
    /// Smart Retry algoritmaları burayı günceller.
    /// </summary>
    public DateTime? NextRetryDate { get; set; }

    /// <summary>
    /// Nexi'den dönen son hata mesajı veya kodu.
    /// Örn: "Insufficient Funds", "Card Expired".
    /// </summary>
    public string? LastPaymentFailureReason { get; set; }

    #endregion

    // Navigation
    public virtual ICollection<Subscription> Subscriptions { get; set; }
    public virtual ICollection<Invoice> Invoices { get; set; }
}