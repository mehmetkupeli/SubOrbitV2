using SubOrbitV2.Domain.Abstractions;

namespace SubOrbitV2.Domain.Entities.Billing;

/// <summary>
/// Faturaya ait her bir kalem/satır.
/// Ürün adı, birim fiyat ve vergi oranı buraya kopyalanır (Snapshot).
/// </summary>
public class InvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Bu satır hangi abonelikten kaynaklandı? (Opsiyonel, tek seferlik satış olabilir).
    /// </summary>
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// Raporlama için Ürün ID referansı.
    /// </summary>
    public Guid? ProductId { get; set; }

    #region Snapshot Verileri

    /// <summary>
    /// Satır Açıklaması.
    /// Örn: "Muhasebe Paketi (1 Ocak - 1 Şubat)" veya "Kalan Kullanım İadesi".
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Bu satırın kapsadığı dönem başlangıcı.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Bu satırın kapsadığı dönem bitişi.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    #endregion

    #region Finansallar

    /// <summary>
    /// Adet.
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Birim Fiyat (Vergisiz).
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Bu satıra uygulanan indirim tutarı.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Vergi Oranı (%). Örn: 25.00
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Hesaplanan Vergi Tutarı.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Satır Toplamı (Vergiler dahil).
    /// Formül: ((UnitPrice * Quantity) - DiscountAmount) + TaxAmount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    #endregion

    /// <summary>
    /// Bu satır bir Kıstelyevm (Proration) hesabı mı?
    /// True ise: Ara dönem farkıdır.
    /// </summary>
    public bool IsProration { get; set; } = false;

    // Navigation
    public virtual Invoice Invoice { get; set; }
}