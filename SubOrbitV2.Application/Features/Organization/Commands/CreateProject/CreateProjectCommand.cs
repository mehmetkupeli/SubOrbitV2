using MediatR;
using Microsoft.AspNetCore.Http;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Organization.Commands.CreateProject;

public record CreateProjectCommand : IRequest<Result<CreateProjectResponse>>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IFormFile? Logo { get; init; }
    public string? WebhookUrl { get; init; }

    // Projenin Kalbi: Ayarlar (İsteğe bağlı, sonradan da doldurulabilir)
    public BillingConfig? BillingConfig { get; init; }
    public SmtpConfiguration? SmtpConfig { get; init; }
}

public record CreateProjectResponse(Guid ProjectId, string ApiKey, string Name);