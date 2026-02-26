using SubOrbitV2.Domain.Abstractions;

namespace SubOrbitV2.Domain.Entities.Catalog;

/// <summary>
/// Özelliklerin (Features) mantıksal veya görsel olarak gruplandığı tablo.
/// Örn: "Fatura Ayarları", "Kullanıcı Limitleri".
/// </summary>
public class FeatureGroup : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Grubun adı (Örn: "Muhasebe Modülü").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// İsteğe bağlı açıklama.
    /// </summary>
    public string? Description { get; set; }

    // Navigation Property
    public virtual ICollection<Feature> Features { get; set; } = new List<Feature>();
}