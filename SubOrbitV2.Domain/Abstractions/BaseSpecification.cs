using System.Linq.Expressions;

namespace SubOrbitV2.Domain.Abstractions;

/// <summary>
/// ISpecification arayüzünü uygulayan soyut temel sınıf.
/// Tüm özel sorgu sınıfları (Specifications) bu sınıftan türeyecektir.
/// Boilerplate (tekrarlayan) kodları azaltmak için yardımcı metotlar içerir.
/// </summary>
/// <typeparam name="T">Sorgulanacak Entity tipi.</typeparam>
public abstract class BaseSpecification<T> : ISpecification<T>
{
    #region Properties Implementation

    public Expression<Func<T, bool>> Criteria { get; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>> OrderBy { get; private set; }
    public Expression<Func<T, object>> OrderByDescending { get; private set; }
    public int Take { get; private set; }
    public int Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Filtresiz (Tüm kayıtları getiren) bir sorgu oluşturur.
    /// </summary>
    protected BaseSpecification()
    {
    }

    /// <summary>
    /// Belirli bir kritere (Where) göre filtreleyen sorgu oluşturur.
    /// </summary>
    /// <param name="criteria">Filtreleme ifadesi (Expression).</param>
    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    #endregion

    #region Helper Methods (Yardımcılar)

    /// <summary>
    /// Sorguya Eager Loading (Join) ekler.
    /// </summary>
    /// <param name="includeExpression">Dahil edilecek navigation property.</param>
    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Sorguya string tabanlı Eager Loading ekler (Derinlemesine ilişkiler için).
    /// </summary>
    /// <param name="includeString">Dahil edilecek yol. Örn: "Lines.Product"</param>
    protected virtual void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Sonuçları artan sırada (A-Z, 0-9) sıralar.
    /// </summary>
    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Sonuçları azalan sırada (Z-A, 9-0) sıralar.
    /// </summary>
    protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Sonuçlara sayfalama (Pagination) uygular.
    /// </summary>
    /// <param name="skip">Atlanacak kayıt sayısı.</param>
    /// <param name="take">Alınacak kayıt sayısı.</param>
    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    #endregion
}