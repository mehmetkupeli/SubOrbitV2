using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;

namespace SubOrbitV2.Domain.Specifications.Catalog;

/// <summary>
/// Belirli bir projede, belirli bir ürüne ait belirli bir özelliğin 
/// mevcut değerini (var olup olmadığını) bulur.
/// </summary>
public class ProductFeatureValueSpecification : BaseSpecification<ProductFeatureValue>
{
    public ProductFeatureValueSpecification(Guid projectId, Guid productId, Guid featureId)
        : base(x => x.ProjectId == projectId && x.ProductId == productId && x.FeatureId == featureId)
    {
    }
}