using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Entities.Catalog;

/// <summary>
/// Bir ürünün (Product) satışa sunulan fiyat ve süre seçeneği.
/// Örn: Startup Paketi için Aylık 100 DKK.
/// </summary>
public class Price : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }
    public Guid ProductId { get; set; }

    /// <summary>
    /// Planın görünür adı (Faturada yazacak).
    /// Örn: "Startup Aylık Abonelik".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Net Fiyat Tutarı (Vergiler hariç taban fiyat).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Para birimi. Varsayılan: DKK.
    /// </summary>
    public string Currency { get; set; } = "DKK";

    /// <summary>
    /// Bu fiyata uygulanacak varsayılan KDV/Vergi oranı (%).
    /// Örn: 25.00
    /// Not: Global satışlarda bu oran müşterinin ülkesine göre ezilebilir.
    /// </summary>
    public decimal VatRate { get; set; }

    /// <summary>
    /// Fatura kesim aralığı (Ay, Yıl).
    /// </summary>
    public BillingInterval Interval { get; set; }

    /// <summary>
    /// Döngü sayısı.
    /// Örn: Interval=Month, Count=3 -> "3 Ayda Bir" (Quarterly).
    /// </summary>
    public int IntervalCount { get; set; } = 1;

    /// <summary>
    /// Deneme süresi (Gün). 
    /// </summary>
    public int TrialDays { get; set; } = 0;

    /// <summary>
    /// Satışa açık mı?
    /// </summary>
    public bool IsActive { get; set; } = true;

    // --- Navigation ---
    public virtual Product? Product { get; set; }
}