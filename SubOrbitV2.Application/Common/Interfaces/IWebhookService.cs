using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Catalog;

namespace SubOrbitV2.Application.Common.Interfaces;

/// <summary>
/// Sistem içerisindeki önemli iş olaylarını (Domain Events / Abonelik başlangıcı vb.) 
/// algılayıp, dış sistemlere (Müşterinin API'sine) Webhook olarak fırlatılmasını 
/// koordine eden yönetici servis arayüzüdür.
/// </summary>
public interface IWebhookService
{
    #region Notification Methods

    /// <summary>
    /// Bir abonenin (Payer) bir pakete (Product) başarıyla ödeme yaptığını ve 
    /// erişim yetkisi (Access Granted) kazandığını dış sisteme bildirir.
    /// </summary>
    /// <param name="projectId">İlgili projenin ID'si.</param>
    /// <param name="payer">Ödemeyi yapan müşteri nesnesi.</param>
    /// <param name="product">Satın alınan ürün/paket nesnesi.</param>
    /// <param name="validUntil">Erişimin geçerli olduğu son tarih.</param>
    /// <param name="features">Pakete ait limit ve özellikler listesi.</param>
    Task NotifyAccessGrantedAsync(Guid projectId, Payer payer, Product product, DateTime validUntil, List<ProductFeatureValue> features);


    #endregion
}