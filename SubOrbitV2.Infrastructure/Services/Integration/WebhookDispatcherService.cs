using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Domain.Entities.Integration;
using SubOrbitV2.Domain.Enums;
using SubOrbitV2.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace SubOrbitV2.Infrastructure.Services.Integration;

public class WebhookDispatcherService : IWebhookDispatcherService
{
    private readonly ApplicationDbContext _dbContext; // Background job olduğu için DbContext'i direkt kullanmak daha güvenlidir
    private readonly IBackgroundJobClient _jobClient;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookDispatcherService> _logger;

    public WebhookDispatcherService(
        ApplicationDbContext dbContext,
        IBackgroundJobClient jobClient,
        HttpClient httpClient,
        ILogger<WebhookDispatcherService> logger)
    {
        _dbContext = dbContext;
        _jobClient = jobClient;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task ProcessWebhookEventAsync(Guid webhookEventId)
    {
        #region 1. Veri Hazırlığı
        // Global Query Filter'ı eziyoruz çünkü arka plan servisinin aktif bir 'CurrentProject' bağlamı yoktur.
        var evt = await _dbContext.WebhookEvents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == webhookEventId);

        if (evt == null || evt.Status == WebhookEventStatus.Success) return;

        var project = await _dbContext.Projects
            .IgnoreQueryFilters()
            .Include(p => p.Settings)
            .FirstOrDefaultAsync(x => x.Id == evt.ProjectId);

        if (project == null || string.IsNullOrEmpty(project.Settings?.WebhookUrl))
        {
            evt.Status = WebhookEventStatus.Failed;
            evt.LastError = "Projenin Webhook URL'si tanımlanmamış.";
            await _dbContext.SaveChangesAsync();
            return;
        }
        #endregion

        #region 2. Gönderim İşlemi (HTTP & HMAC)
        evt.Status = WebhookEventStatus.Processing;
        var attempt = new WebhookDeliveryAttempt { WebhookEventId = evt.Id, TargetUrl = project.Settings.WebhookUrl };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // İmzayı oluştur (Güvenlik)
            var secret = project.Settings.WebhookSecret ?? project.ApiKey;
            var signature = ComputeHmacSha256(evt.Payload, secret);

            var request = new HttpRequestMessage(HttpMethod.Post, attempt.TargetUrl);
            request.Content = new StringContent(evt.Payload, Encoding.UTF8, "application/json");

            request.Headers.Add("X-SubOrbit-Event", evt.EventType);
            request.Headers.Add("X-SubOrbit-Signature", signature);

            var response = await _httpClient.SendAsync(request);
            stopwatch.Stop();

            attempt.ResponseStatusCode = (int)response.StatusCode;
            attempt.DurationMs = (long)stopwatch.Elapsed.TotalMilliseconds;

            if (response.IsSuccessStatusCode)
            {
                evt.Status = WebhookEventStatus.Success;
            }
            else
            {
                attempt.ResponseBody = await response.Content.ReadAsStringAsync();
                HandleRetryLogic(evt);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            attempt.ResponseStatusCode = 500;
            attempt.ResponseBody = ex.Message;
            attempt.DurationMs = (long)stopwatch.Elapsed.TotalMilliseconds;
            HandleRetryLogic(evt);
        }
        #endregion

        #region 3. Kayıt İşlemi
        await _dbContext.WebhookDeliveryAttempts.AddAsync(attempt);
        await _dbContext.SaveChangesAsync();
        #endregion
    }

    private void HandleRetryLogic(WebhookEvent evt)
    {
        evt.RetryCount++;
        if (evt.RetryCount >= 10) // Maksimum 10 deneme
        {
            evt.Status = WebhookEventStatus.Failed;
            evt.LastError = "Maksimum deneme sayısına ulaşıldı.";
        }
        else
        {
            evt.Status = WebhookEventStatus.Retrying;
            // Exponential Backoff (2, 4, 8, 16... dakika)
            var delayMinutes = Math.Pow(2, evt.RetryCount);
            evt.NextRetryDate = DateTime.UtcNow.AddMinutes(delayMinutes);

            // Yeni denemeyi Hangfire'a zamanla
            _jobClient.Schedule<IWebhookDispatcherService>(
                x => x.ProcessWebhookEventAsync(evt.Id),
                TimeSpan.FromMinutes(delayMinutes));

            _logger.LogWarning("Webhook teslim edilemedi. Olay ID: {EventId}. {Delay} dakika sonra tekrar denenecek.", evt.Id, delayMinutes);
        }
    }

    private string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}