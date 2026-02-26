namespace SubOrbitV2.Application.Common.Models;

/// <summary>
/// ProjectSetting.EncryptedBillingConfig alanından deserialize edilecek yapı.
/// Her proje (Tenant) kendi Nexi/Ödeme anahtarlarını burada tutar.
/// </summary>
public class BillingConfig
{
    /// <summary>
    /// Canlı Ortam Gizli Anahtarı (Backend işlemleri için).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Canlı Ortam Ödeme Sayfası Anahtarı (Frontend/Hosted Page için).
    /// </summary>
    public string CheckoutKey { get; set; } = string.Empty;
}