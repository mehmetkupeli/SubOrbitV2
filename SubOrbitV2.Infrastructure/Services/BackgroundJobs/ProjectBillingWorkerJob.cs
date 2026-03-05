using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Features.Billing.Commands.ProcessRecurringBilling;

namespace SubOrbitV2.Infrastructure.Services.BackgroundJobs;
public class ProjectBillingWorkerJob : IProjectBillingWorkerJob
{
    private readonly ISender _sender; // MediatR

    public ProjectBillingWorkerJob(ISender sender)
    {
        _sender = sender;
    }

    public async Task ProcessProjectAsync(Guid projectId)
    {
        // Senin yazdığın o devasa Handler'ı tetikleyen yer tam olarak burası!
        var command = new ProcessProjectRecurringBillingCommand(projectId);
        await _sender.Send(command);
    }
}