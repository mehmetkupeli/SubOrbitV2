using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;

namespace SubOrbitV2.Domain.Specifications.Catalog;

/// <summary>
/// Bir ürünü çekerken, ürüne tanımlanmış özellikleri (FeatureValues) 
/// ve o özelliklerin tanımlarını (Feature) derinlemesine yükler (Eager Loading).
/// </summary>
public class ProductWithFeaturesSpecification : BaseSpecification<Product>
{
    public ProductWithFeaturesSpecification(Guid projectId, Guid productId)
        : base(x => x.ProjectId == projectId && x.Id == productId)
    {
        AddInclude(x => x.FeatureValues);
        AddInclude("FeatureValues.Feature");
    }
}