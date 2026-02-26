namespace SubOrbitV2.Domain.Enums;

public enum InvoiceStatus
{
    /// <summary>
    /// Taslak aşamasında. Henüz kesinleşmedi, hesaplamalar yapılıyor.
    /// Müşteriye gösterilmez.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Kesinleşti, müşteriye gönderildi, ödeme bekleniyor.
    /// (Nexi'den çekim yapılmadan hemen önceki an).
    /// </summary>
    Open = 1,

    /// <summary>
    /// Ödeme başarıyla alındı.
    /// </summary>
    Paid = 2,

    /// <summary>
    /// Ödeme alınamadı (Kart limiti yetersiz vs.). Dunning sürecine girer.
    /// </summary>
    PastDue = 3, // Vadesi Geçmiş

    /// <summary>
    /// Hata sonucu iptal edildi. Geçersiz.
    /// </summary>
    Void = 4,

    /// <summary>
    /// Tahsil edilemedi ve artık vazgeçildi (Şüpheli alacak).
    /// </summary>
    Uncollectible = 5
}
