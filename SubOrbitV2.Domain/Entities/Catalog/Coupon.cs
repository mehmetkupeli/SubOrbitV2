using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Entities.Catalog;

/// <summary>
/// Abonelik kalemlerine (SubscriptionItem) uygulanabilecek indirim tanımları.
/// </summary>
public class Coupon : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Müşterinin gireceği promosyon kodu (Örn: YAZ2025).
    /// Büyük harfe zorlanmalı ve Unique olmalıdır.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// İndirim türü (Yüzde veya Sabit Tutar).
    /// </summary>
    public CouponDiscountType DiscountType { get; set; }

    /// <summary>
    /// İndirim değeri (%20 için 20, 50 DKK için 50).
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// İndirimin ne kadar süreyle uygulanacağı.
    /// </summary>
    public CouponDuration Duration { get; set; }

    /// <summary>
    /// Eğer Duration 'Repeating' ise, bu indirim kaç ay boyunca geçerli olacak?
    /// </summary>
    public int? DurationInMonths { get; set; }

    /// <summary>
    /// Kuponun son kullanma tarihi (Genel kampanya bitişi).
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Toplamda kaç kişi bu kuponu kullanabilir? (Kota).
    /// </summary>
    public int? MaxRedemptions { get; set; }

    /// <summary>
    /// Şu ana kadar kaç kez kullanıldı?
    /// </summary>
    public int TimesRedeemed { get; set; } = 0;

    /// <summary>
    /// Kupon şu an aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary> 
    /// Kupon halka açık mı? Uygulama içi satın almada mı geçerli?
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Opsiyonel: Kuponun sadece belirli bir üründe geçerli olması isteniyorsa ilgili ProductId.
    /// Null ise tüm ürünlerde geçerlidir.
    /// </summary>
    public Guid? RestrictedProductId { get; set; }

    // Navigation
    public virtual Product? RestrictedProduct { get; set; }

}