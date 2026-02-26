using SubOrbitV2.Domain.Abstractions;

namespace SubOrbitV2.Domain.Entities.Catalog;

/// <summary>
/// Bir üründe (Product) hangi özelliğin (Feature) hangi değerde olduğunu tutan eşleşme tablosu.
/// Örn: "Startup Paketi" (Product) -> "Kullanıcı Limiti" (Feature) -> "3" (Value).
/// </summary>
public class ProductFeatureValue : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Hangi ürün?
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Hangi özellik?
    /// </summary>
    public Guid FeatureId { get; set; }

    /// <summary>
    /// Özelliğin bu ürün için değeri.
    /// Feature.DataType'a göre yorumlanır.
    /// Boolean -> "true" / "false"
    /// Integer -> "5", "10", "-1" (Sınırsız)
    /// Text -> "Standart", "Premium"
    /// </summary>
    public string Value { get; set; } = string.Empty;

    // --- Navigation Properties ---
    public virtual Product? Product { get; set; }
    public virtual Feature? Feature { get; set; }
}