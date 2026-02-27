using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Enums;
using SubOrbitV2.Infrastructure.Data;
using System.Text.Json;

namespace SubOrbitV2.Infrastructure.Services.Notification;

public class NotificationDispatcherService : INotificationDispatcherService
{
    #region Fields
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailSender _emailSender;
    private readonly IEncryptionService _encryptionService;
    #endregion

    #region Constructor
    public NotificationDispatcherService(
        ApplicationDbContext dbContext,
        IEmailSender emailSender,
        IEncryptionService encryptionService)
    {
        _dbContext = dbContext;
        _emailSender = emailSender;
        _encryptionService = encryptionService;
    }
    #endregion

    #region Public Methods
    public async Task ProcessNotificationQueueAsync(Guid notificationId)
    {
        // 1. Filtreleri yoksayarak Olayı ve Tenant ayarlarını çek
        var notification = await _dbContext.NotificationQueues
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == notificationId);

        if (notification == null || notification.Status == NotificationStatus.Sent) return;

        var projectSettings = await _dbContext.ProjectSettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.ProjectId == notification.ProjectId);

        if (projectSettings == null || string.IsNullOrEmpty(projectSettings.EncryptedSmtpConfig))
        {
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = "Projenin SMTP ayarları bulunamadı.";
            await _dbContext.SaveChangesAsync();
            return;
        }

        // 2. SMTP Şifre Çözümü
        SmtpConfiguration? smtpConfig;
        try
        {
            var json = _encryptionService.Decrypt(projectSettings.EncryptedSmtpConfig);
            smtpConfig = JsonSerializer.Deserialize<SmtpConfiguration>(json);
        }
        catch (Exception)
        {
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = "SMTP Ayarları çözümsüz/hatalı.";
            await _dbContext.SaveChangesAsync();
            return;
        }

        // 3. Gönderim Kararı (Strategy)
        try
        {
            if (notification.Channel == NotificationChannel.Email)
            {
                await _emailSender.SendEmailAsync(
                    smtpConfig!,
                    notification.Recipient,
                    notification.Subject,
                    notification.Body,
                    notification.AttachmentPath);
            }
            // İleride: else if (notification.Channel == Sms) { _smsSender.Send(...) }

            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            notification.RetryCount++;
            notification.ErrorMessage = ex.Message;
            notification.Status = notification.RetryCount >= 3 ? NotificationStatus.Failed : NotificationStatus.Pending;
        }

        await _dbContext.SaveChangesAsync();
    }
    #endregion
}