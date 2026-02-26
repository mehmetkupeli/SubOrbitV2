using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;

namespace SubOrbitV2.Domain.Specifications.Catalog;

/// <summary>
/// Bir projeye ait belirli bir kodla tanımlanmış ürünü bulur.
/// Ürün kodları proje bazında tekil olmalıdır.
/// </summary>
public class ProductByCodeSpecification : BaseSpecification<Product>
{
    public ProductByCodeSpecification(Guid projectId, int code) : base(x => x.ProjectId == projectId && x.Code == code)
    {
    }
}