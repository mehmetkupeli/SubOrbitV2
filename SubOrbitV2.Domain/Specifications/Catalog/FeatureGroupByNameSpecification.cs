using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;

namespace SubOrbitV2.Domain.Specifications.Catalog;

/// <summary>
/// Proje bazlı, isim üzerinden tekillik kontrolü yapar.
/// Not: Kültürel farklılıklardan (Danca, Japonca, Türkçe vb.) etkilenmemek için 
/// karşılaştırma 'ToUpper' üzerinden invariant (kültürsüz) olarak yapılır.
/// </summary>
public class FeatureGroupByNameSpecification : BaseSpecification<FeatureGroup>
{
    public FeatureGroupByNameSpecification(Guid projectId, string name)
        : base(x => x.ProjectId == projectId && x.Name.ToUpper() == name.ToUpper())
    {
    }
}