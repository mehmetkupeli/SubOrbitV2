using SubOrbitV2.Domain.Abstractions;

namespace SubOrbitV2.Domain.Entities.Catalog;

/// <summary>
/// Hangi kuponu, kim, ne zaman kullandı?
/// </summary>
public class CouponRedemption : BaseEntity, IMustHaveProject
{
    public Guid ProjectId { get; set; }

    public Guid CouponId { get; set; }

    public Guid PayerId { get; set; }

    /// <summary>
    /// Kupon spesifik bir abonelikte kullanıldıysa ID'si.
    /// </summary>
    public Guid? SubscriptionId { get; set; }

    /// <summary>
    /// O kullanımda sağlanan indirim tutarı.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Kullanım tarihi.
    /// </summary>
    public DateTime RedeemedAt { get; set; }
}