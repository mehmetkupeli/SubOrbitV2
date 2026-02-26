using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;

namespace SubOrbitV2.Domain.Specifications.Catalog;

/// <summary>
/// Proje içerisinde belirli bir 'Key' (Anahtar) ile tanımlanmış özelliği bulur.
/// Büyük/Küçük harf duyarsızdır (Invariant).
/// </summary>
public class FeatureByKeySpecification : BaseSpecification<Feature>
{
    public FeatureByKeySpecification(Guid projectId, string key)
        : base(x => x.ProjectId == projectId && x.Key.ToUpper() == key.ToUpper())
    {
    }
}