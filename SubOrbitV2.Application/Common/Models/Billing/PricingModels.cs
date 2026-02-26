using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Application.Common.Models.Billing;

/// <summary>
/// Hesaplama motoruna gönderilecek parametreler.
/// </summary>
public record PricingRequest(
    decimal BaseAmount,
    decimal VatRate,
    BillingInterval Interval,
    int IntervalCount,
    BillingAlignmentStrategy AlignmentStrategy,
    int AlignmentDay = 1, // FixedDay seçilirse hangi gün? (Varsayılan 1)
    CouponDiscountType? CouponType = null,
    decimal? CouponValue = null
);

/// <summary>
/// Hesaplama motorundan dönecek kusursuz mali tablo.
/// </summary>
public record PricingResult(
    decimal BaseAmount,       // Ürünün standart fiyatı
    decimal ProratedAmount,   // Kıstelyevm (Hizalama) sonrası asıl tutar
    decimal DiscountAmount,   // Kupon indirimi tutarı
    decimal SubTotal,         // İndirim sonrası KDV hariç tutar
    decimal TaxAmount,        // KDV Tutarı
    decimal FinalTotal,       // NEXI'DEN ÇEKİLECEK NİHAİ TUTAR
    DateTime NextBillingDate  // ABONELİĞİN YENİLECEĞİ O SİHİRLİ TARİH
);