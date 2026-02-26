using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Common.Interfaces;

/// <summary>
/// Tüm bildirim kanallarını (Email, SMS, Push) tek bir noktadan yöneten servis.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// İsteğin içindeki 'Channel' tipine göre ilgili sağlayıcıyı bulur ve gönderim yapar.
    /// </summary>
    Task SendAsync(NotificationRequest request);
}