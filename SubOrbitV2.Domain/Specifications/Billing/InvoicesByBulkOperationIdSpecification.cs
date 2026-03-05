using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Domain.Specifications.Billing;

/// <summary>
/// BulkOperation ID'sine ait, henüz Nexi'den çekilmemiş (Open) faturaları, 
/// müşteri (Payer) kart bilgileriyle birlikte getirir.
/// </summary>
public class InvoicesByBulkOperationIdSpecification : BaseSpecification<Invoice>
{
    public InvoicesByBulkOperationIdSpecification(Guid bulkOperationId) : base(i => i.BulkOperationId == bulkOperationId && i.Status == InvoiceStatus.Open)
    {
        AddInclude(i => i.Payer);
    }
}