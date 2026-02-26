using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Specifications.Catalog;

/// <summary>
/// Bir projede, aynı ürüne ait aynı para biriminde ve periyotta 
/// mükerrer bir fiyatın olup olmadığını kontrol eder.
/// </summary>
public class PriceUniquenessSpecification : BaseSpecification<Price>
{
    public PriceUniquenessSpecification(
        Guid projectId,
        Guid productId,
        string currency,
        BillingInterval interval,
        int intervalCount)
        : base(x =>
            x.ProjectId == projectId &&
            x.ProductId == productId &&
            x.Currency.ToUpper() == currency.ToUpper() &&
            x.Interval == interval &&
            x.IntervalCount == intervalCount)
    {
    }
}