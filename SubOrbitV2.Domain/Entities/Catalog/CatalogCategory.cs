using SubOrbitV2.Domain.Abstractions;

namespace SubOrbitV2.Domain.Entities.Catalog;

/// <summary>
/// Ürünlerin (Product) gruplandığı üst kategori.
/// Örn: "Aylık Abonelikler", "Kredi Paketleri", "Eklentiler".
/// </summary>
public class CatalogCategory : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Yazılımcıların API entegrasyonunda kullanacağı sabit sayısal kod.
    /// Örn: 101, 500, 501.
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Kategori adı (Örn: "SaaS Paketleri").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Bu kategori ve altındaki ürünler satışa açık mı?
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation Property
    public virtual ICollection<Product> Products { get; set; }
}