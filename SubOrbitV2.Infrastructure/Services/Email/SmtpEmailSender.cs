using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Infrastructure.Services.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;

    // Artık IOptions<MailSettings> inject etmiyoruz!
    public SmtpEmailSender(ILogger<SmtpEmailSender> logger)
    {
        _logger = logger;
    }

    public async Task SendEmailAsync(SmtpConfiguration config, string to, string subject, string htmlBody)
    {
        try
        {
            var email = new MimeMessage();

            // Config'den gelen gönderici bilgisi
            email.From.Add(new MailboxAddress(config.DisplayName, config.FromEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlBody };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            // Config'den gelen sunucu bilgileriyle bağlan
            await smtp.ConnectAsync(config.Host, config.Port, config.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            // Kimlik doğrulama
            if (!string.IsNullOrEmpty(config.UserName) && !string.IsNullOrEmpty(config.Password))
            {
                await smtp.AuthenticateAsync(config.UserName, config.Password);
            }

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {To} via host {Host}", to, config.Host);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} using host {Host}", to, config.Host);
            // Burada throw etmiyoruz, iş akışı (fatura oluşumu) bozulmasın diye.
            // Ama NotificationStatus = Failed olarak işaretlenecek logic Application katmanında kurulacak.
        }
    }
}