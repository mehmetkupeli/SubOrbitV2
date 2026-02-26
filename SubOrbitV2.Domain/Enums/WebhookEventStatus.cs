namespace SubOrbitV2.Domain.Enums;

public enum WebhookEventStatus
{
    /// <summary>
    /// Kuyruğa alındı, gönderilmeyi bekliyor.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Şu an işleniyor / gönderiliyor.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Başarıyla iletildi (Karşı sunucudan 2xx kodu alındı).
    /// </summary>
    Success = 2,

    /// <summary>
    /// Tüm denemeler başarısız oldu (Artık denenmeyecek).
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Hata aldı ama tekrar denenecek (Backoff sürecinde).
    /// </summary>
    Retrying = 4
}