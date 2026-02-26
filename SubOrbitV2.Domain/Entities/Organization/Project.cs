using SubOrbitV2.Domain.Abstractions;

namespace SubOrbitV2.Domain.Entities.Organization;

/// <summary>
/// Bir Tenant'a bağlı, bağımsız çalışabilen uygulama/proje birimi.
/// Örn: Isank Muhasebe, Isank IK.
/// </summary>
public class Project : BaseEntity
{
    public Guid TenantId { get; set; }

    // Proje Adı
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Firmanın logosunun dosya yolu veya URL'i.
    /// </summary>
    public string? LogoUrl { get; set; }

    // Açıklama
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// API isteklerinde kimlik doğrulaması için kullanılacak benzersiz anahtar.
    /// Indexlenmeli ve Unique olmalıdır.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // --- Navigation Properties ---
    public virtual Tenant? Tenant { get; set; }

    // 1-to-1 İlişki: Ayarlar
    public virtual ProjectSetting? Settings { get; set; }
}