using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;

namespace SubOrbitV2.Domain.Specifications.Catalog;

/// <summary>
/// Bir projeye ait belirli bir kod ile kategori aramak için kullanılır.
/// Uniqueness (Tekillik) kontrolü için kritik öneme sahiptir.
/// </summary>
public class CatalogCategoryByCodeSpecification : BaseSpecification<CatalogCategory>
{
    public CatalogCategoryByCodeSpecification(Guid projectId, int code) : base(x => x.ProjectId == projectId && x.Code == code)
    {
    }
}