using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;

namespace SubOrbitV2.Domain.Entities.Identity;

/// <summary>
/// Abonelerin (Payer) şifresiz giriş yapabilmesi için oluşturulan
/// kısa ömürlü, tek kullanımlık geçiş bileti (Magic Link Token).
/// </summary>
public class PortalToken : BaseEntity
{
    /// <summary>
    /// Bu bilet hangi cüzdan/abone için kesildi?
    /// </summary>
    public Guid PayerId { get; set; }

    /// <summary>
    /// URL içinde gönderilecek benzersiz, tahmin edilemez anahtar.
    /// Veritabanında Indexlenmelidir.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token'ın geçerlilik süresinin dolduğu an (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Bu token kullanıldı mı? Tekrar kullanımı (Replay Attack) engellemek için.
    /// </summary>
    public bool IsRedeemed { get; set; } = false;

    /// <summary>
    /// Eğer kullanıldıysa, ne zaman kullanıldı? (Audit için).
    /// </summary>
    public DateTime? RedeemedAt { get; set; }

    // --- Navigation Properties ---
    public virtual Payer? Payer { get; set; }
}