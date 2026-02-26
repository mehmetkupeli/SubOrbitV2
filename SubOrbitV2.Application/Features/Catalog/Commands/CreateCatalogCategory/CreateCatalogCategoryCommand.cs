using MediatR;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateCatalogCategory;

/// <summary>
/// Yeni bir katalog kategorisi oluşturma isteği.
/// Not: Entity içerisinde 'Description' alanı yoktur, 'Code' alanı zorunludur.
/// </summary>
public record CreateCatalogCategoryCommand : IRequest<Result<CreateCatalogCategoryResponse>>
{
    #region Properties
    /// <summary>
    /// Yazılımcıların API entegrasyonunda kullanacağı sabit sayısal kod.
    /// </summary>
    public int Code { get; init; }

    /// <summary>
    /// Kategori adı.
    /// </summary>
    public string Name { get; init; } = string.Empty;
    #endregion
}
