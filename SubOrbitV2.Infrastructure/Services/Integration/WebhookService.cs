using Hangfire;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models.Webhooks;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Entities.Integration;
using SubOrbitV2.Domain.Enums;
using System.Text.Json;

namespace SubOrbitV2.Infrastructure.Services.Integration;

/// <summary>
/// IWebhookService arayüzünün implementasyonudur.
/// İş olaylarını (Domain Events) Strongly-Typed payload'lara çevirir, 
/// Outbox Pattern mantığıyla veritabanına yazar ve Hangfire üzerinden asenkron gönderimi tetikler.
/// </summary>
public class WebhookService : IWebhookService
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _jobClient;

    #endregion

    #region Constructor

    public WebhookService(IUnitOfWork unitOfWork, IBackgroundJobClient jobClient)
    {
        _unitOfWork = unitOfWork;
        _jobClient = jobClient;
    }

    #endregion

    #region Public Methods (İş Akışları)

    public async Task NotifyAccessGrantedAsync(Guid projectId, Payer payer, Product product, DateTime validUntil, List<ProductFeatureValue> features)
    {
        // 1. Strongly-Typed Payload Hazırlığı (Müşterinin beklediği format)
        var payload = new AccessWebhookPayload
        {
            Event = "access.granted",
            Timestamp = DateTime.UtcNow,
            User = new WebhookUser
            {
                ExternalId = payer.ExternalId,
                Email = payer.Email
            },
            Access = new WebhookAccessDetails
            {
                Status = "Active",
                PlanName = product.Name,
                PlanId = product.Id.ToString(),
                ValidUntil = validUntil,
                // Müşteriye sıfır çağrı entegrasyonu (Zero-Call Integration) sağlamak için 
                // satın alınan paketin tüm özelliklerini (Feature) direkt paketin içine gömüyoruz.
                Features = features
                    .Where(pf => pf.Feature != null && !string.IsNullOrEmpty(pf.Feature.Key))
                    .Select(pf => new WebhookFeatureDto
                    {
                        Code = pf.Feature!.Key,
                        Name = pf.Feature.Name,
                        Value = pf.Value
                    })
                    .ToList()
            }
        };

        // 2. İşlemi kaydet ve tetikle
        await TriggerEventAsync(projectId, payload.Event, payload);
    }

    #endregion

    #region Private Helper Methods (Ortak Yardımcılar)

    /// <summary>
    /// Hazırlanan payload'u JSON formatına çevirir, veritabanına taslak (Pending) olarak kaydeder 
    /// ve Hangfire üzerinden arka plan dağıtım (Dispatcher) görevini tetikler.
    /// </summary>
    private async Task TriggerEventAsync(Guid projectId, string eventType, object payload)
    {
        // API standartlarına uygun olması için camelCase JSON serileştirme ayarı
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var webhookEvent = new WebhookEvent
        {
            ProjectId = projectId,
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload, jsonOptions),
            Status = WebhookEventStatus.Pending,
            NextRetryDate = DateTime.UtcNow
        };

        // Olayı Veritabanı kuyruğuna (Unit of Work) ekliyoruz.
        // DİKKAT: CommandHandler tarafındaki Fatura, Payer ve Cüzdan güncellemeleriyle BİRLİKTE
        // aynı Transaction içerisinde atomik olarak mühürlenir. (Outbox Pattern)
        await _unitOfWork.Repository<WebhookEvent>().AddAsync(webhookEvent);

        // Veritabanı değişikliklerini tek hamlede mühürle
        await _unitOfWork.SaveChangesAsync();

        // Veri veritabanına mühürlendikten SONRA Hangfire Job'ını tetikliyoruz.
        // Bu sayede transaction fail olursa boş yere webhook gönderilmemiş olur.
        _jobClient.Enqueue<IWebhookDispatcherService>(x => x.ProcessWebhookEventAsync(webhookEvent.Id));
    }

    #endregion
}