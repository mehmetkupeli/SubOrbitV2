using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog; // Price ve Product için
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Entities.Billing;

/// <summary>
/// Müşterinin (Payer) sahip olduğu her bir ürün/hizmet aboneliği.
/// Payer ile N-1 ilişkisindedir. Faturalama burada başlar.
/// </summary>
public class Subscription : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    #region İlişkiler (Relations)

    /// <summary>
    /// Aboneliğin sahibi olan ana cüzdan/müşteri.
    /// </summary>
    public Guid PayerId { get; set; }

    /// <summary>
    /// Satın alınan ürün.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Satın alınan fiyat planı (Tutar, Süre, Döngü).
    /// </summary>
    public Guid PriceId { get; set; }

    /// <summary>
    /// (Varsa) Uygulanan indirim kuponu.
    /// </summary>
    public Guid? ActiveCouponId { get; set; }

    #endregion

    #region Kimlik ve Tanımlama (Identification)

    /// <summary>
    /// Müşterinin kendi sistemindeki referans ID'si.
    /// Örn: Muhasebecinin "Mükellef Firma ID'si" (COMP-99).
    /// Index: ProjectId + PayerId + ExternalId -> Unique olabilir.
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Abonelik için kullanıcı dostu etiket/açıklama.
    /// Örn: "Ahmet Ltd. Şti. Lisansı" veya "Merkez Ofis Depolama".
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Bu abonelik, Payer'ın "Ana Aboneliği" mi?
    /// True ise: Payer'ın sisteme giriş yetkisini belirleyen temel pakettir.
    /// False ise: Ek hizmet veya alt müşteri lisansıdır.
    /// </summary>
    public bool IsMain { get; set; } = false;

    #endregion

    #region Zamanlama ve Döngü (Cycle & Alignment)

    /// <summary>
    /// Aboneliğin ilk başladığı tarih.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Mevcut hizmet döneminin başlangıcı.
    /// Örn: 1 Ocak.
    /// </summary>
    public DateTime CurrentPeriodStart { get; set; }

    /// <summary>
    /// Mevcut hizmet döneminin bitişi.
    /// Örn: 1 Şubat.
    /// </summary>
    public DateTime CurrentPeriodEnd { get; set; }

    /// <summary>
    /// Bir sonraki fatura kesim tarihi.
    /// Payer'ın 'BillingCycleAnchor' (Hizalama Günü) ile senkronize çalışır.
    /// </summary>
    public DateTime NextBillingDate { get; set; }

    /// <summary>
    /// İptal edildiyse, iptal edildiği tarih.
    /// </summary>
    public DateTime? CanceledAt { get; set; }

    #endregion

    #region Finansal Durum (Financials)

    /// <summary>
    /// Adet (Örn: 5 Kullanıcı, 10 Lisans).
    /// Varsayılan: 1.
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Bu aboneliğe özel yerel bakiye (Kumbarası).
    /// Paket düşürme (Downgrade) veya bu kaleme özel iadeler burada birikir.
    /// Sıradaki faturada önce burası harcanır.
    /// </summary>
    public decimal VirtualBalance { get; set; } = 0;

    #endregion

    #region Durum (Status)

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    #endregion

    // Navigation Properties
    public virtual Payer Payer { get; set; }
    public virtual Price Price { get; set; }
    public virtual Coupon? ActiveCoupon { get; set; }
}