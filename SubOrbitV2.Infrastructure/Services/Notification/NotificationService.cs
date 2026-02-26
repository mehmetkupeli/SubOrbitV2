using Microsoft.Extensions.Logging;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Infrastructure.Services.Notification;

public class NotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IEmailSender emailSender, ILogger<NotificationService> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendAsync(NotificationRequest request)
    {
        _logger.LogInformation("Processing notification via channel: {Channel} to {Recipient}", request.Channel, request.Recipient);

        switch (request.Channel)
        {
            case NotificationChannel.Email:
                await SendEmailAsync(request);
                break;

            case NotificationChannel.Sms:
                _logger.LogWarning("SMS Provider not implemented yet.");
                break;

            default:
                _logger.LogError("Unsupported notification channel: {Channel}", request.Channel);
                break;
        }
    }

    private async Task SendEmailAsync(NotificationRequest request)
    {
        if (request.SmtpConfig == null)
        {
            _logger.LogError("SMTP Config is missing for Email notification.");
            return;
        }

        await _emailSender.SendEmailAsync(request.SmtpConfig, request.Recipient, request.Subject, request.Body);
    }
}