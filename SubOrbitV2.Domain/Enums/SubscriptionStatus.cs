namespace SubOrbitV2.Domain.Enums;

public enum SubscriptionStatus
{
    /// <summary>
    /// Aboneliği beklemede.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Aktif ve ödemesi düzenli alınıyor.
    /// </summary>
    Active = 1,

    /// <summary>
    /// İptal edildi (Süresi bitene kadar hizmet alabilir).
    /// </summary>
    Canceled = 2,

    /// <summary>
    /// Ödeme alınamadı, borçlu durumda (Grace Period).
    /// </summary>
    PastDue = 3,

    /// <summary>
    /// Süresi doldu ve hizmet durduruldu.
    /// </summary>
    Expired = 4,

    /// <summary>
    /// Geçici olarak donduruldu (Askıya alındı).
    /// </summary>
    Suspended = 5
}