using SubOrbitV2.Application.Common.Models.Payment;

namespace SubOrbitV2.Application.Common.Interfaces;

/// <summary>
/// Nexi ödeme altyapısı ile iletişim kuran istemci arayüzü.
/// </summary>
public interface INexiClient
{
    /// <summary>
    /// Yeni bir ödeme sayfası (Hosted Page) başlatır.
    /// Abonelik veya tek çekim işlemlerinde kullanıcıyı yönlendireceğimiz URL'i ve ödeme referansını içerir.
    /// </summary>
    Task<NexiPaymentResponse?> InitializePaymentAsync(NexiOrderItemDto orderItem);

    /// <summary>
    /// Başlatılmış bir ödemenin detaylarını ve güncel durumunu getirir.
    /// </summary>
    Task<NexiPaymentDetailsResponse?> GetPaymentDetailsAsync(string paymentId);

    /// <summary>
    /// Toplu ödeme işlemini başlatır.
    /// Binlerce aboneyi tek seferde tahsilata göndermek için kullanılır. İşlemin takip ID'sini (BulkId) döner.
    /// </summary>
    Task<string?> BulkChargeSubscriptionsAsync(IEnumerable<BulkSubscriptionItem> items, Guid projectId);

    /// <summary>
    /// Başlatılan toplu tahsilat (Bulk Charge) işleminin sonucunu ve durumunu sorgular.
    /// </summary>
    Task<BulkStatusResponse?> RetrieveBulkChargeStatusAsync(string bulkId, int pageNumber = 1);

    /// <summary>
    /// Kullanıcının kayıtlı kartını (Aboneliğini) güncellemesi için 0 tutarlı bir doğrulama oturumu açar.
    /// </summary>
    Task<NexiPaymentResponse?> CreateCardUpdateSessionAsync(string subscriptionId, string returnUrl, string termsUrl);

    /// <summary>
    /// Kayıtlı abonelik (SubscriptionId) üzerinden arka planda doğrudan tekli para çeker.
    /// </summary>
    Task<(bool Success, string? ChargeId)> ChargeSubscriptionAsync(string nexiSubscriptionId, NexiOrderItem orderItem, string currency, Guid myReference);
}