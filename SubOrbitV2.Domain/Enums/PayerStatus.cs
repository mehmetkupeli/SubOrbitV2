namespace SubOrbitV2.Domain.Enums;

public enum PayerStatus
{
    /// <summary>
    /// Payer oluşturuldu ancak ilk ödeme henüz tamamlanmadı.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Hesap aktif, ödemeleri düzenli alınıyor.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Ödeme alınamadı (Retry sürecinde). Hizmet kısıtlı devam edebilir.
    /// </summary>
    PastDue = 2,

    /// <summary>
    /// Ödeme denemeleri tükendi, borçlu durumda. Hizmet durduruldu.
    /// </summary>
    Unpaid = 3,

    /// <summary>
    /// Müşteri kendi isteğiyle iptal etti veya sistem tarafından banlandı.
    /// </summary>
    Canceled = 4
}