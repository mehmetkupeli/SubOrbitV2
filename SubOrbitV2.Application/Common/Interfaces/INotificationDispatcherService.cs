namespace SubOrbitV2.Application.Common.Interfaces;

/// <summary>
/// Hangfire tarafından arka planda tetiklenip, NotificationQueue tablosundaki 
/// bekleyen (Pending) bildirimleri ilgili projenin (Tenant) ayarlarıyla fırlatır.
/// </summary>
public interface INotificationDispatcherService
{
    Task ProcessNotificationQueueAsync(Guid notificationId);
}