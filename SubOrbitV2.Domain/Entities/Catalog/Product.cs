using SubOrbitV2.Domain.Abstractions;

namespace SubOrbitV2.Domain.Entities.Catalog;

/// <summary>
/// Müşteriye sunulan ana ürün veya hizmet paketi.
/// Fiyat ve süre bağımsızdır (Onlar Price tablosunda).
/// Örn: "Startup Paketi" (Code: 100), "Enterprise Paket" (Code: 900).
/// </summary>
public class Product : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Bu ürün hangi kategoride listelenecek?
    /// (Örn: Web Paketleri kategorisi).
    /// </summary>
    public Guid CatalogCategoryId { get; set; }

    /// <summary>
    /// Yazılımcıların kod içinde referans vereceği sabit sayısal kod.
    /// Unique olmalı (Proje bazında).
    /// Örn: 100 (Basic), 200 (Pro).
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Ürünün görünen adı (Örn: "Professional Paket").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Pazarlama açıklaması.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Satışa açık mı? False ise listelerde çıkmaz.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Ürün gizli mi? (Örn: Özel teklifler için).
    /// True ise Public API listelerinde dönmez ama direkt ID ile satılabilir.
    /// </summary>
    public bool IsHidden { get; set; } = false;

    // --- Navigation Properties ---

    // Kategorisi
    public virtual CatalogCategory? CatalogCategory { get; set; }

    // Özellik Değerleri (Henüz yazmadık)
    public virtual ICollection<ProductFeatureValue> FeatureValues { get; set; }
}