using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;

namespace SubOrbitV2.Domain.Specifications.Billing;

/// <summary>
/// Müşteriyi (Payer) kendi sistemimizdeki ID'si üzerinden bulurken,
/// geçmiş ve mevcut tüm aboneliklerini (Subscriptions) de beraberinde getirir.
/// Yeniden kullanım (Reuse) kontrollerinde aktif abonelik tespiti için gereklidir.
/// </summary>
public class PayerWithSubscriptionsByExternalIdSpecification : BaseSpecification<Payer>
{
    public PayerWithSubscriptionsByExternalIdSpecification(Guid projectId, string externalId)
        : base(x => x.ProjectId == projectId && x.ExternalId == externalId)
    {
        AddInclude(x => x.Subscriptions);
    }
}