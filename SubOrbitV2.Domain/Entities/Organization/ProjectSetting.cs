using SubOrbitV2.Domain.Abstractions;

namespace SubOrbitV2.Domain.Entities.Organization;

/// <summary>
/// Projeye ait hassas ve operasyonel ayarların tutulduğu tablo.
/// Project tablosundan ayrılarak güvenlik ve performans sağlanmıştır.
/// </summary>
public class ProjectSetting : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Webhook gönderilirken payload'ı imzalamak (HMAC) için kullanılan gizli anahtar.
    /// </summary>
    public string? WebhookSecret { get; set; }

    // Veritabanında şifreli (Encrypted) saklanacak.
    // İçerik: Payment Provider (Nexi/Stripe) API Keyleri
    public string? EncryptedBillingConfig { get; set; }

    // İçerik: SMTP sunucu bilgileri ve şifresi
    public string? EncryptedSmtpConfig { get; set; }

    // --- Navigation ---
    public virtual Project? Project { get; set; }
}