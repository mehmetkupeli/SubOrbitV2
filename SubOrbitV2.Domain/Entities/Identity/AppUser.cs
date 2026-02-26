using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Organization;

namespace SubOrbitV2.Domain.Entities.Identity;

/// <summary>
/// Sistem yöneticileri ve Tenant yetkilileri için kullanılan kullanıcı kimliği.
/// Bu kullanıcılar panele e-posta ve parola ile giriş yaparlar.
/// </summary>
public class AppUser : BaseEntity
{
    /// <summary>
    /// Kullanıcının bağlı olduğu ana firma.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Kullanıcının adı soyadı.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Giriş için kullanılan e-posta adresi.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Güvenli (hashlenmiş) parola.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Kullanıcı aktif mi? Pasife çekilirse panele giriş yapamaz.
    /// </summary>
    public bool IsActive { get; set; } = true;

    #region Refresh Token Fields
    /// <summary>
    /// Kullanıcının oturumunu yenilemek için kullanılan uzun ömürlü anahtar.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Refresh Token'ın son kullanma tarihi.
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }
    #endregion

    // --- Navigation Properties ---
    public virtual Tenant? Tenant { get; set; }
}