using Microsoft.EntityFrameworkCore;
using SubOrbitV2.Domain.Abstractions;

namespace SubOrbitV2.Infrastructure.Persistence;

/// <summary>
/// Domain katmanında tanımlanan "Specification" (Sorgu Tarifi) nesnelerini işleyerek,
/// Entity Framework Core'un anlayacağı IQueryable sorgularına dönüştüren motordur.
/// </summary>
/// <typeparam name="TEntity">Sorgulanacak Entity tipi.</typeparam>
public class SpecificationEvaluator<TEntity> where TEntity : BaseEntity
{
    #region Query Generation Logic

    /// <summary>
    /// Başlangıç sorgusuna (inputQuery) specification içindeki kuralları uygular.
    /// </summary>
    /// <param name="inputQuery">Ham IQueryable sorgusu (Genelde DbContext.Set<T>()).</param>
    /// <param name="specification">Uygulanacak kurallar bütünü.</param>
    /// <returns>Filtrelenmiş, sıralanmış ve ilişkileri dahil edilmiş sorgu.</returns>
    public static IQueryable<TEntity> GetQuery(IQueryable<TEntity> inputQuery, ISpecification<TEntity> specification)
    {
        var query = inputQuery;

        // 1. Filtering (Where)
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // 2. Includes (Join - Expression Based)
        // Örn: .Include(x => x.Payer)
        query = specification.Includes.Aggregate(query,
            (current, include) => current.Include(include));

        // 3. Include Strings (Join - String Based for Nested)
        // Örn: .Include("InvoiceLines.Product")
        query = specification.IncludeStrings.Aggregate(query,
            (current, include) => current.Include(include));

        // 4. Sorting (OrderBy / OrderByDescending)
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // 5. Pagination (Skip & Take)
        // Önemli: Sayfalama her zaman en sonda yapılmalıdır!
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }

    #endregion
}