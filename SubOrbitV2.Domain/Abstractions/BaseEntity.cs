using System.ComponentModel.DataAnnotations.Schema;

namespace SubOrbitV2.Domain.Abstractions;

/// <summary>
/// Tüm Entity sınıflarının türediği temel sınıf.
/// Kimlik (Identity), Denetim (Audit), Yumuşak Silme (Soft Delete) ve Olay (Domain Event) yeteneklerini barındırır.
/// </summary>
public abstract class BaseEntity
{
    #region Identity (Kimlik)

    /// <summary>
    /// Entity'nin benzersiz kimliği (Primary Key).
    /// Varsayılan olarak yeni bir GUID atanır.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    #endregion

    #region Audit (Denetim İzleri)

    /// <summary>
    /// Kaydın oluşturulma zamanı (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; } // Bu alanı eklemeliyiz

    /// <summary>
    /// Kaydın son güncellenme zamanı (UTC).
    /// İlk oluştuğunda null olabilir veya CreatedAt ile aynı olabilir.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    public Guid? LastModifiedBy { get; set; } // Bu alanı eklemeliyiz

    #endregion

    #region Soft Delete (Yumuşak Silme)

    /// <summary>
    /// Kaydın silinmiş olup olmadığını belirtir.
    /// True ise kayıt silinmiş (Soft Deleted) kabul edilir, ancak veritabanında fiziksel olarak durur.
    /// Sorgularda varsayılan olarak filtrelenmelidir (Global Query Filter).
    /// </summary>
    public bool IsDeleted { get; set; }

    #endregion

    #region Domain Events (Olay Yönetimi)

    /// <summary>
    /// Entity üzerinde gerçekleşen olayların geçici listesi.
    /// Veritabanına kaydedilmez ([NotMapped]), sadece bellekte yaşar.
    /// </summary>
    [NotMapped]
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Dış dünyadan olaylara sadece okuma amaçlı erişim sağlar.
    /// Listeyi manipüle etmek için aşağıdaki metotlar kullanılmalıdır.
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Entity'ye yeni bir olay ekler.
    /// Örn: Sipariş oluşturulduğunda "OrderCreatedEvent" eklenir.
    /// </summary>
    /// <param name="domainEvent">Fırlatılacak olay nesnesi.</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Eklenmiş bir olayı listeden çıkarır.
    /// </summary>
    /// <param name="domainEvent">Çıkarılacak olay nesnesi.</param>
    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Bekleyen tüm olayları temizler.
    /// Genellikle olaylar veritabanına kaydedildikten (Dispatch) sonra çağrılır.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    #endregion
}