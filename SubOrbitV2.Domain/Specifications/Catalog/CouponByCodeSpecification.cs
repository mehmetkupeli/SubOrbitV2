using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;

namespace SubOrbitV2.Domain.Specifications.Catalog;

/// <summary>
/// Proje içerisinde belirli bir promosyon koduna sahip kuponu bulur.
/// Kontrol işlemi büyük/küçük harf duyarsız (Invariant) yapılır.
/// </summary>
public class CouponByCodeSpecification : BaseSpecification<Coupon>
{
    public CouponByCodeSpecification(Guid projectId, string code)
        : base(x => x.ProjectId == projectId && x.Code.ToUpper() == code.ToUpper())
    {
    }
}