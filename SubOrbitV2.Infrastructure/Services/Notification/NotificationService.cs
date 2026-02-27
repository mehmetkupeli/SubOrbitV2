using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Communication;
using SubOrbitV2.Domain.Entities.Organization;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Infrastructure.Services.Notification;

public class NotificationService : INotificationService
{
    #region Fields
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _jobClient;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<NotificationService> _logger;
    #endregion

    #region Constructor
    public NotificationService(
        IUnitOfWork unitOfWork,
        IBackgroundJobClient jobClient,
        IWebHostEnvironment env,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _jobClient = jobClient;
        _env = env;
        _logger = logger;
    }
    #endregion

    #region Public Methods
    public async Task NotifyInvoiceCreatedAsync(Guid projectId, Payer payer, Invoice invoice, Project project, string pdfPath)
    {
        // 1. Şablon (Template) Okuma
        var templatePath = Path.Combine(_env.WebRootPath ?? _env.ContentRootPath, "templates", "email", "InvoiceCreatedTemplate.html");
        string htmlBody;

        if (File.Exists(templatePath))
        {
            htmlBody = await File.ReadAllTextAsync(templatePath);
            // Dinamik Değişkenleri Enjekte Etme
            htmlBody = htmlBody.Replace("{{CustomerName}}", payer.Name)
                               .Replace("{{ProjectName}}", project.Name)
                               .Replace("{{InvoiceNumber}}", invoice.Number)
                               .Replace("{{TotalAmount}}", $"{invoice.TotalAmount:N2} {invoice.Currency}");
        }
        else
        {
            // Şablon dosyası bulunamazsa Fallback (Güvenlik ağı)
            htmlBody = $"<h3>Merhaba {payer.Name},</h3><p>{project.Name} hizmetinize ait <b>{invoice.Number}</b> numaralı faturanızı ekte bulabilirsiniz.</p>";
            _logger.LogWarning("Email template not found at {Path}", templatePath);
        }

        // 2. Kuyruğa Kayıt (Outbox Pattern)
        var notification = new NotificationQueue
        {
            ProjectId = projectId,
            Recipient = payer.Email,
            Channel = NotificationChannel.Email,
            Subject = $"{project.Name} - Faturanız ve Abonelik Bilgileriniz",
            Body = htmlBody,
            AttachmentPath = pdfPath,
            Status = NotificationStatus.Pending,
            ScheduledTime = DateTime.UtcNow
        };

        await _unitOfWork.Repository<NotificationQueue>().AddAsync(notification);
        await _unitOfWork.SaveChangesAsync(); // Değişikliği mühürle

        // 3. Arka Plan İşlemini Tetikle (Hangfire)
        _jobClient.Enqueue<INotificationDispatcherService>(x => x.ProcessNotificationQueueAsync(notification.Id));
    }
    #endregion
}