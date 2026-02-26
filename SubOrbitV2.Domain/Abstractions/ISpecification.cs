using System.Linq.Expressions;

namespace SubOrbitV2.Domain.Abstractions;

/// <summary>
/// Veritabanı sorgularını (Query Logic) kapsülleyen temel arayüz.
/// Generic Repository ile birlikte kullanılarak, sorgu mantığını repository'den ayırır.
/// </summary>
/// <typeparam name="T">Sorgulanacak Entity tipi.</typeparam>
public interface ISpecification<T>
{
    #region Filtering (Filtreleme)

    /// <summary>
    /// Sorgunun WHERE koşulunu belirtir.
    /// Örn: x => x.IsActive && x.Amount > 100
    /// </summary>
    Expression<Func<T, bool>> Criteria { get; }

    #endregion

    #region Eager Loading (İlişkili Veriler)

    /// <summary>
    /// Sorguya dahil edilecek ilişkili tabloların listesi (Include).
    /// Örn: Invoice çekerken Payer ve InvoiceLines da gelsin.
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Sorguya string olarak dahil edilecek ilişkili tablolar (Nested Include için).
    /// Örn: "InvoiceLines.Product"
    /// </summary>
    List<string> IncludeStrings { get; }

    #endregion

    #region Sorting (Sıralama)

    /// <summary>
    /// Artan (Ascending) sıralama ifadesi.
    /// </summary>
    Expression<Func<T, object>> OrderBy { get; }

    /// <summary>
    /// Azalan (Descending) sıralama ifadesi.
    /// </summary>
    Expression<Func<T, object>> OrderByDescending { get; }

    #endregion

    #region Pagination (Sayfalama)

    /// <summary>
    /// Atlanacak kayıt sayısı (SQL OFFSET).
    /// </summary>
    int Take { get; }

    /// <summary>
    /// Alınacak kayıt sayısı (SQL LIMIT).
    /// </summary>
    int Skip { get; }

    /// <summary>
    /// Sayfalama yapılıp yapılmayacağını belirtir.
    /// </summary>
    bool IsPagingEnabled { get; }

    #endregion
}