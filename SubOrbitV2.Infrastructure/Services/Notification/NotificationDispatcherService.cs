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
    private readonly ILogger<NotificationDispatcherService> _logger;
    #endregion

    #region Constructor
    public NotificationDispatcherService(
        ApplicationDbContext dbContext,
        IEmailSender emailSender,
        IEncryptionService encryptionService,
        ILogger<NotificationDispatcherService> logger)
    {
        _dbContext = dbContext;
        _emailSender = emailSender;
        _encryptionService = encryptionService;
        _logger = logger;
    }
    #endregion

    #region Public Methods
    public async Task ProcessNotificationQueueAsync(Guid notificationId)
    {
        // 1. Veriyi Getir (Global filtreleri ez, background job context'i yok)
        var notification = await _dbContext.NotificationQueues
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == notificationId);

        if (notification == null || notification.Status == NotificationStatus.Sent) return;

        // 2. Projenin SMTP ayarlarını çek
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

        // 3. Şifreyi Çöz ve Ayarları Hazırla
        SmtpConfiguration? smtpConfig;
        try
        {
            var json = _encryptionService.Decrypt(projectSettings.EncryptedSmtpConfig);
            smtpConfig = JsonSerializer.Deserialize<SmtpConfiguration>(json);
        }
        catch (Exception ex)
        {
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = $"SMTP Ayarları çözülemedi: {ex.Message}";
            await _dbContext.SaveChangesAsync();
            return;
        }

        // 4. Gönderim İşlemi
        try
        {
            if (notification.Channel == NotificationChannel.Email)
            {
                await _emailSender.SendEmailAsync(
                    smtpConfig!,
                    notification.Recipient,
                    notification.Subject,
                    notification.Body,
                    notification.AttachmentPath); // Fatura PDF'ini ekliyoruz
            }

            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            notification.RetryCount++;
            notification.ErrorMessage = ex.Message;

            // Basit bir retry mantığı, 3 kereden sonra iptal
            notification.Status = notification.RetryCount >= 3 ? NotificationStatus.Failed : NotificationStatus.Pending;
        }

        await _dbContext.SaveChangesAsync();
    }
    #endregion
}