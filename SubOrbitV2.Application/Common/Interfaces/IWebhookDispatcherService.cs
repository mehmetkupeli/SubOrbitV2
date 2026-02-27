namespace SubOrbitV2.Application.Common.Interfaces;

public interface IWebhookDispatcherService
{
    /// <summary>
    /// Hangfire tarafından arka planda tetiklenir. İlgili olayı HTTP Post ile karşı tarafa fırlatır.
    /// </summary>
    Task ProcessWebhookEventAsync(Guid webhookEventId);
}