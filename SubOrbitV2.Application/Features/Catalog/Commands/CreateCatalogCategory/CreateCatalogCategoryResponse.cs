namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateCatalogCategory;


/// <summary>
/// Kategori oluşturma sonrası dönülen yanıt.
/// </summary>
public record CreateCatalogCategoryResponse(Guid Id, int Code, string Name);
