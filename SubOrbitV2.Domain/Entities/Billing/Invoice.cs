using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Entities.Billing;

/// <summary>
/// Kesilen faturanın başlık (Header) bilgileri.
/// Snapshot mantığıyla çalışır; oluşturulduğu andaki müşteri ve adres bilgilerini kopyalar.
/// Değiştirilemez (Immutable) bir mali kayıttır.
/// </summary>
public class Invoice : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    #region İlişkiler ve Kimlik

    /// <summary>
    /// Faturanın kesildiği müşteri (Cüzdan).
    /// </summary>
    public Guid PayerId { get; set; }

    /// <summary>
    /// Fatura Numarası. Sıralı ve benzersiz olmalıdır (Örn: INV-2026-0001).
    /// Kod veya DB Sequence tarafından üretilir.
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Faturanın oluşma sebebi (Döngü, Güncelleme, Manuel).
    /// </summary>
    public InvoiceBillingReason BillingReason { get; set; }

    #endregion

    #region Snapshot (Müşteri Bilgileri Kopyası)

    // Müşteri (Payer) tablosundaki veriler değişse bile faturadaki bu veriler sabit kalmalıdır.

    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerTaxOffice { get; set; }
    public string? CustomerTaxNumber { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerCity { get; set; }
    public string? CustomerCountry { get; set; }

    #endregion

    #region Dönem ve Tarihler

    /// <summary>
    /// Hizmetin başladığı tarih.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Hizmetin bittiği tarih.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Ödemenin başarıyla alındığı tarih.
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Son ödeme tarihi (Vade).
    /// </summary>
    public DateTime? DueDate { get; set; }

    #endregion

    #region Finansallar (Toplamlar)

    public string Currency { get; set; } = "DKK";

    /// <summary>
    /// Ara Toplam (Vergiler ve İndirimler öncesi saf tutar).
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Uygulanan toplam indirim tutarı.
    /// </summary>
    public decimal TotalDiscount { get; set; }

    /// <summary>
    /// Toplam Vergi tutarı.
    /// </summary>
    public decimal TotalTax { get; set; }

    /// <summary>
    /// Genel Toplam (Ödenecek Tutar).
    /// Formül: Subtotal - TotalDiscount + TotalTax.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Cüzdan bakiyesinden (Wallet/VirtualBalance) düşülen tutar.
    /// </summary>
    public decimal AmountCredited { get; set; }

    /// <summary>
    /// Kredi Kartı veya Ödeme Yöntemi ile tahsil edilen gerçek tutar.
    /// </summary>
    public decimal AmountPaid { get; set; }

    /// <summary>
    /// Kalan borç (Kısmi ödeme varsa). 0 ise tamamen ödenmiştir.
    /// </summary>
    public decimal AmountRemaining { get; set; }

    #endregion

    #region Entegrasyon ve Durum

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    /// <summary>
    /// Nexi veya ödeme sağlayıcıdan dönen işlem ID'si.
    /// </summary>
    public string? NexiTransactionId { get; set; }

    /// <summary>
    /// Faturanın PDF linki (Varsa).
    /// </summary>
    public string? HostedInvoiceUrl { get; set; }

    /// <summary>
    /// Başarısızlık durumunda hata mesajı.
    /// </summary>
    public string? FailureMessage { get; set; }

    /// <summary>
    /// Faturanın oluşturulan PDF dosyasının fiziksel yolu veya S3/Blob URL'i.
    /// Örn: "/invoices/2026/02/INV-101.pdf"
    /// </summary>
    public string? PdfPath { get; set; }

    #endregion

    // Navigation
    public virtual ICollection<InvoiceLine> Lines { get; set; }
    public virtual Payer Payer { get; set; }
}