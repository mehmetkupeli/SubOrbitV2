using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;

namespace SubOrbitV2.Domain.Specifications.Billing;

/// <summary>
/// Müşterinin kendi sistemindeki ID'si (ExternalId) üzerinden
/// SubOrbit tarafındaki Payer kaydını bulur.
/// </summary>
public class PayerByExternalIdSpecification : BaseSpecification<Payer>
{
    public PayerByExternalIdSpecification(Guid projectId, string externalId)
        : base(x => x.ProjectId == projectId && x.ExternalId == externalId)
    {
    }
}