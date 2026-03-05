using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Entities.Billing;

/// <summary>
/// Nexi vb. sağlayıcılara yapılan toplu işlemlerin (Batch) takibi.
/// Senaryo: Bir Payer'ın 5000 aboneliği için tek bir Bulk Request atılır, 
/// dönen BulkId ile işlemin sonucu (Polling) takip edilir.
/// </summary>
public class BulkOperation : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    /// <summary>
    /// İşlemin yapıldığı ana müşteri (Cüzdan).
    /// </summary>
    public Guid PayerId { get; set; }

    /// <summary>
    /// Nexi tarafından dönen toplu işlem ID'si.
    /// Bu ID ile sorgulama (Query) yapılır.
    /// </summary>
    public string? ExternalBulkId { get; set; }

    /// <summary>
    /// Bu paketin içinde kaç adet işlem var? (Örn: 50 Abonelik yenilemesi).
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// İşlemin durumu.
    /// </summary>
    public BulkOperationStatus Status { get; set; } = BulkOperationStatus.Pending;

    /// <summary>
    /// Nexi'den dönen ham cevap (JSON). Debug için saklanır.
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// Bir sonraki sorgulama (Polling) zamanı.
    /// Job, bu zamana gelmiş ve bitmemiş işlemleri tarar.
    /// </summary>
    public DateTime? NextCheckTime { get; set; }

    /// <summary>
    /// Kaç kez sorgulama yapıldı?
    /// </summary>
    public int CheckCount { get; set; } = 0;
}