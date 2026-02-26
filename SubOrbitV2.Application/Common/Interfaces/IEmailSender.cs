using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Common.Interfaces;

public interface IEmailSender
{
    /// <summary>
    /// Verilen SMTP konfigürasyonunu kullanarak e-posta gönderir.
    /// </summary>
    /// <param name="config">Proje bazlı SMTP ayarları.</param>
    /// <param name="to">Alıcı adresi.</param>
    /// <param name="subject">Konu.</param>
    /// <param name="htmlBody">İçerik.</param>
    Task SendEmailAsync(SmtpConfiguration config, string to, string subject, string htmlBody);
}