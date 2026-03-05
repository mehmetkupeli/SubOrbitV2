namespace SubOrbitV2.Application.Common.Interfaces;

public interface INexiBulkDispatcherJob
{
    /// <summary>
    /// Belirtilen BulkOperationId altındaki tüm faturaları toplayıp Nexi'ye fırlatır.
    /// </summary>
    Task ProcessBulkChargeAsync(Guid bulkOperationId);
}