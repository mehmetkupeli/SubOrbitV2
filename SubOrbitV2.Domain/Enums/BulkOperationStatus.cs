namespace SubOrbitV2.Domain.Enums;

public enum BulkOperationStatus
{
    Pending = 0,    // Nexi'ye gönderilmeyi bekliyor
    Processing = 1, // Gönderildi, BulkId alındı, sonuç bekleniyor
    Completed = 2,  // Tüm işlemler başarıyla sonuçlandı
    PartiallyFailed = 3, // Bazıları ödendi, bazıları hata aldı
    Failed = 4      // Genel bir hata oluştu (API erişimi vb.)
}