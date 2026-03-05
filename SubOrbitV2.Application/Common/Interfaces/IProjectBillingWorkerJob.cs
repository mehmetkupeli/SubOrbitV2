namespace SubOrbitV2.Application.Common.Interfaces;
public interface IProjectBillingWorkerJob
{
    // Master'ın her proje için çağıracağı işçi
    Task ProcessProjectAsync(Guid projectId);
}