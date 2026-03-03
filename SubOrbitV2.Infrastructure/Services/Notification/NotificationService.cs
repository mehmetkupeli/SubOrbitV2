using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<NotificationService> _logger;
    #endregion

    #region Constructor
    public NotificationService(
        IUnitOfWork unitOfWork,
        IBackgroundJobClient jobClient,
        IWebHostEnvironment env,
        IHttpContextAccessor httpContextAccessor,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _jobClient = jobClient;
        _env = env;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    #endregion

    #region Public Methods
    public async Task<Guid> NotifyWelcomeAndSubscriptionAsync(Guid projectId, Payer payer, Invoice invoice, Project project, string pdfPath, bool isProrated = false)
    {
        var subject = $"🎉 {project.Name}'na Hoş Geldiniz! Aboneliğiniz Başladı (Fatura: {invoice.Number})";
        return await PrepareAndQueueNotificationAsync("WelcomeAndInvoiceTemplate.html", subject, projectId, payer, invoice, project, pdfPath, isProrated);
    }

    public async Task<Guid> NotifyStandardInvoiceAsync(Guid projectId, Payer payer, Invoice invoice, Project project, string pdfPath)
    {
        var subject = $"{project.Name} - {invoice.CreatedAt:MMMM yyyy} Faturanız ({invoice.Number})";
        return await PrepareAndQueueNotificationAsync("StandardInvoiceTemplate.html", subject, projectId, payer, invoice, project, pdfPath);
    }

    public void DispatchBackgroundJob(Guid notificationId)
    {
        _jobClient.Enqueue<INotificationDispatcherService>(x => x.ProcessNotificationQueueAsync(notificationId));
    }
    #endregion

    #region Private Helper Methods
    private async Task<Guid> PrepareAndQueueNotificationAsync(string templateName, string subject, Guid projectId, Payer payer, Invoice invoice, Project project, string pdfPath, bool isProrated = false)
    {
        var templatePath = Path.Combine(_env.ContentRootPath, "Templates", "Email", templateName);
        string htmlBody;

        // 1. Dinamik Base URL Tespiti (Logoların mailde kırık çıkmaması için zorunlu)
        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = request != null ? $"{request.Scheme}://{request.Host.Value}" : "https://suborbitapi.multillo.com";

        // 2. Dinamik Logo veya İsim HTML'inin Hazırlanması
        string projectLogoOrNameHtml = string.IsNullOrEmpty(project.LogoUrl)
            ? $"<h2 style='color:#333; margin:0; font-family:sans-serif;'>{project.Name}</h2>"
            : $"<img src='{baseUrl}{project.LogoUrl}' alt='{project.Name} Logo' style='max-height:50px;' />";

        #region Proration (Kısmi Ödeme) Bilgi Kutusu Zekası
        string prorationNoticeHtml = "";

        // SADECE kısmi ödeme varsa ve adam bir hizalama seçtiyse bu kutuyu üret
        if (isProrated && payer.AlignmentStrategy != BillingAlignmentStrategy.None)
        {
            string alignmentReason = payer.AlignmentStrategy switch
            {
                BillingAlignmentStrategy.FixedDay => $"Fatura kesim tarihinizi her ayın <strong>{payer.BillingAnchorDay}. günü</strong> olarak belirlediğiniz için",
                BillingAlignmentStrategy.CalendarQuarter => "<strong>Çeyrek Dönem (1 Ocak, 1 Nisan vb.)</strong> hizalaması seçtiğiniz için",
                BillingAlignmentStrategy.CalendarHalfYear => "<strong>Yarı Yıl (1 Ocak, 1 Temmuz)</strong> hizalaması seçtiğiniz için",
                BillingAlignmentStrategy.CalendarYear => "<strong>Takvim Yılı (1 Ocak)</strong> hizalaması seçtiğiniz için",
                _ => ""
            };

            if (!string.IsNullOrEmpty(alignmentReason))
            {
                prorationNoticeHtml = $@"
                <div style='background-color: #f8fbff; border-left: 4px solid #0056b3; padding: 15px; margin: 25px 0; border-radius: 4px;'>
                    <h4 style='margin: 0 0 8px 0; color: #0056b3; font-size: 14px;'>ℹ️ Kısmi Ödeme Bilgilendirmesi</h4>
                    <p style='margin: 0; font-size: 13px; color: #444; line-height: 1.6;'>
                        {alignmentReason}, ilk faturanız <strong>{invoice.PeriodStart:dd.MM.yyyy}</strong> ile <strong>{invoice.PeriodEnd:dd.MM.yyyy}</strong> tarihleri arası için kısmi (indirimli) olarak hesaplanmıştır. 
                        Bir sonraki faturanız standart paket tutarı üzerinden <strong>{invoice.PeriodEnd:dd.MM.yyyy}</strong> tarihinde kesilecektir.
                    </p>
                </div>";
            }
        }
        #endregion

        // 3. Şablonun Okunması ve Doldurulması
        if (File.Exists(templatePath))
        {
            htmlBody = await File.ReadAllTextAsync(templatePath);
            htmlBody = htmlBody.Replace("{{ProjectLogoOrName}}", projectLogoOrNameHtml)
                               .Replace("{{ProrationNotice}}", prorationNoticeHtml) 
                               .Replace("{{CustomerName}}", payer.Name)
                               .Replace("{{ProjectName}}", project.Name)
                               .Replace("{{InvoiceNumber}}", invoice.Number)
                               .Replace("{{TotalAmount}}", $"{invoice.TotalAmount:N2} {invoice.Currency}")
                               .Replace("{{InvoiceDate}}", invoice.CreatedAt.ToString("dd.MM.yyyy"));
        }
        else
        {
            htmlBody = $"<h3>Merhaba {payer.Name},</h3><p>{project.Name} hizmetinize ait <b>{invoice.Number}</b> numaralı faturanızı ekte bulabilirsiniz.</p>";
            _logger.LogWarning("Email template not found at {Path}", templatePath);
        }

        // 4. Memory'e (Unit Of Work) Kayıt
        var notification = new NotificationQueue
        {
            ProjectId = projectId,
            Recipient = payer.Email,
            Channel = NotificationChannel.Email,
            Subject = subject,
            Body = htmlBody,
            AttachmentPath = pdfPath,
            Status = NotificationStatus.Pending,
            ScheduledTime = DateTime.UtcNow
        };

        await _unitOfWork.Repository<NotificationQueue>().AddAsync(notification);
        return notification.Id;
    }
    #endregion
}