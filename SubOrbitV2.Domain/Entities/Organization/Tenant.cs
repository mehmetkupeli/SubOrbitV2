using SubOrbitV2.Domain.Abstractions;

namespace SubOrbitV2.Domain.Entities.Organization;

/// <summary>
/// Sistemi kullanan ana Müşteri/Firma (Örn: Isank Software A.Ş.).
/// Organizasyonun en tepesindeki kök varlıktır.
/// </summary>
public class Tenant : BaseEntity
{
    /// <summary>
    /// Firmanın resmi ticari ünvanı.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Firmanın logosunun dosya yolu veya URL'i.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Fatura süreçleri için Vergi/KDV numarası.
    /// </summary>
    public string? TaxNumber { get; set; }

    /// <summary>
    /// Sistemsel bildirimlerin gideceği ana e-posta adresi.
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Firma aktif mi? False ise altındaki hiçbir proje API'ye erişemez.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Açık adres satırı.
    /// </summary>
    public string? Address { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    
    // Navigation Property
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}