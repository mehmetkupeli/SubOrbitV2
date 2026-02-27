using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Specifications.Billing;

/// <summary>
/// Belirli bir aboneliğe (Subscription) ait, henüz ödenmemiş (Open) 
/// ve en güncel (Latest) taslak faturayı bulmak için kullanılır.
/// Webhook aşamasında doğru faturayı güvenle yakalamamızı sağlar.
/// </summary>
public class InvoiceBySubscriptionIdSpecification : BaseSpecification<Invoice>
{
    public InvoiceBySubscriptionIdSpecification(Guid projectId, Guid subscriptionId) :
        base(x =>
            x.ProjectId == projectId &&
            x.Status == InvoiceStatus.Open &&
            x.Lines.Any(l => l.SubscriptionId == subscriptionId))
    {
        #region Includes
        AddInclude(x => x.Lines);
        #endregion

        #region Sorting
        ApplyOrderByDescending(x => x.CreatedAt);
        #endregion
    }
}