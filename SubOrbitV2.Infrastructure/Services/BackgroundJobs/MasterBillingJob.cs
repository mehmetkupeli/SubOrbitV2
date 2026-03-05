using Hangfire;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Organization;

namespace SubOrbitV2.Infrastructure.Services.BackgroundJobs;

public class MasterBillingJob : IMasterBillingJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public MasterBillingJob(IUnitOfWork unitOfWork, IBackgroundJobClient backgroundJobClient)
    {
        _unitOfWork = unitOfWork;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task TriggerAllProjectsAsync()
    {
        // Sistemdeki tüm projeleri (veya sadece aktif olanları) getir
        var projects = await _unitOfWork.Repository<Project>().ListAllAsync();

        foreach (var project in projects)
        {
            _backgroundJobClient.Enqueue<IProjectBillingWorkerJob>(
                worker => worker.ProcessProjectAsync(project.Id)
            );
        }
    }
}

