using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Entities.Catalog;

/// <summary>
/// Sistemin sunabileceği yeteneklerin tanımlandığı kütüphane.
/// Örn: 'invoice_create', 'user_limit'.
/// Değerleri (Value) burada değil, ProductFeatureValue tablosunda tutulur.
/// </summary>
public class Feature : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Bu özelliğin bağlı olduğu görsel grup.
    /// </summary>
    public Guid FeatureGroupId { get; set; }

    /// <summary>
    /// Kod içinde kontrol yapılacak benzersiz anahtar.
    /// Örn: 'invoice_create', 'max_users'.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// UI'da müşteriye gösterilecek ad.
    /// Örn: "Fatura Oluşturma", "Personel Sayısı".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Bu özelliğin veri tipi (Boolean, Integer vs.)
    /// Eşleştirme yapılırken değerin formatını belirler.
    /// </summary>
    public FeatureDataType DataType { get; set; }

    /// <summary>
    /// UI için açıklama metni.
    /// </summary>
    public string? Description { get; set; }

    // --- Navigation Properties ---
    public virtual FeatureGroup? FeatureGroup { get; set; }
}