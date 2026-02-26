using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Application.Common.Models;

/// <summary>
/// Sistemin herhangi bir kanal üzerinden göndereceği bildirimi temsil eder.
/// </summary>
public class NotificationRequest
{
    /// <summary>
    /// Bildirimin tipi (Email, Sms, Push, Webhook).
    /// </summary>
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Alıcı Bilgisi (Email adresi veya Telefon numarası).
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Konu Başlığı (Sadece Email ve Push için anlamlı, SMS'te boştur).
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Mesajın gövdesi (HTML veya Plain Text).
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Eğer mail atılacaksa, hangi proje ayarlarıyla atılacağı.
    /// SMS ise hangi başlıkla atılacağı vb. config.
    /// </summary>
    public SmtpConfiguration? SmtpConfig { get; set; } // Şimdilik sadece SMTP var.
}