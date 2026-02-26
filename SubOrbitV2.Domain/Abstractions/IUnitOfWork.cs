namespace SubOrbitV2.Domain.Abstractions;

/// <summary>
/// Veritabanı işlemlerini toplu bir transaction (işlem) olarak yöneten birim.
/// Repository'lere tek bir noktadan erişim sağlar ve değişiklikleri atomik olarak kaydeder.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// İstenilen Entity tipi için Generic Repository örneğini döner.
    /// Eğer daha önce oluşturulmuşsa cache'den getirir, yoksa yeni oluşturur.
    /// </summary>
    /// <typeparam name="TEntity">Repository'si istenen Entity tipi.</typeparam>
    IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;

    /// <summary>
    /// Tüm değişiklikleri (Transaction) veritabanına kaydeder.
    /// Domain Event'ler bu aşamada tetiklenir.
    /// </summary>
    /// <param name="cancellationToken">İptal token'ı.</param>
    /// <returns>Etkilenen kayıt sayısı.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}