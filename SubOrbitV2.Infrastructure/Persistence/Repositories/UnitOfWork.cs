using System.Collections;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Infrastructure.Data;

namespace SubOrbitV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// IUnitOfWork arayüzünün Entity Framework Core implementasyonu.
/// Repository instance'larını yönetir ve DbContext üzerinden transaction sağlar.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    #region Fields

    private readonly ApplicationDbContext _context;

    // Repository instance'larını tutacağımız basit bir cache (Hafıza)
    private Hashtable _repositories;

    // Dispose edildi mi kontrolü
    private bool _disposed;

    #endregion

    #region Constructor

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    #endregion

    #region Repository Management

    public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        // Cache yoksa oluştur
        if (_repositories == null)
            _repositories = new Hashtable();

        // Entity'nin tip adını al (Örn: "Invoice")
        var type = typeof(TEntity).Name;

        // Cache'de bu repository var mı?
        if (!_repositories.ContainsKey(type))
        {
            // Yoksa GenericRepository tipini belirle
            var repositoryType = typeof(GenericRepository<>);

            // Yeni bir instance oluştur (GenericRepository<Invoice>(_context))
            var repositoryInstance = Activator.CreateInstance(
                repositoryType.MakeGenericType(typeof(TEntity)),
                _context);

            // Cache'e ekle
            _repositories.Add(type, repositoryInstance);
        }

        // Cache'den dön
        return (IGenericRepository<TEntity>)_repositories[type]!;
    }

    #endregion

    #region Transaction Management

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // DbContext içindeki dispatcher burada tetiklenecek.
        return await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Cleanup (Dispose)

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }
        _disposed = true;
    }

    #endregion
}