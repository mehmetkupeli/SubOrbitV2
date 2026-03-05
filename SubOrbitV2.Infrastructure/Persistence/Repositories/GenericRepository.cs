using Microsoft.EntityFrameworkCore;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Infrastructure.Data;

namespace SubOrbitV2.Infrastructure.Persistence.Repositories;

/// <summary>
/// IGenericRepository arayüzünün Entity Framework Core implementasyonu.
/// SpecificationEvaluator kullanarak dinamik sorgu oluşturur.
/// </summary>
/// <typeparam name="T">Entity Tipi</typeparam>
public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    #region Fields & Constructor

    protected readonly ApplicationDbContext _context;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    #endregion

    #region Standard CRUD Implementation

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<IReadOnlyList<T>> ListAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
    }

    public void Update(T entity)
    {
        _context.Set<T>().Update(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _context.Set<T>().AddRangeAsync(entities);
    }

    public void UpdateRange(IEnumerable<T> entities)
    {
        _context.Set<T>().UpdateRange(entities);
    }

    public void SoftDelete(T entity)
    {
        entity.IsDeleted = true;
        Update(entity);
    }

    public void HardDelete(T entity)
    {
        _context.Set<T>().Remove(entity);
    }

    #endregion

    #region Specification Methods Implementation

    public async Task<T?> GetEntityWithSpec(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).ToListAsync();
    }

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).CountAsync();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// SpecificationEvaluator'ı çağırarak IQueryable oluşturur.
    /// Kod tekrarını önlemek için merkezi bir metot.
    /// </summary>
    private IQueryable<T> ApplySpecification(ISpecification<T> spec)
    {
        // DbContext.Set<T>() -> Tabloyu seçer
        // ApplySpecification -> Where, Include, OrderBy uygular
        return SpecificationEvaluator<T>.GetQuery(_context.Set<T>().AsQueryable(), spec);
    }

    #endregion
}