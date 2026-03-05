namespace SubOrbitV2.Application.Common.Interfaces;

public interface INexiStatusCheckerJob
{
    Task CheckBulkStatusAsync(Guid projectId, Guid bulkOperationId);
}