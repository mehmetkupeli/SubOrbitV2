using MediatR;
using Microsoft.AspNetCore.Http;
using SubOrbitV2.Application.Common.Models;
using System.Text.Json.Serialization;

namespace SubOrbitV2.Application.Features.Organization.Commands.UpdateProject;

public record UpdateProjectCommand : IRequest<Result<bool>>
{
    #region Temel Proje Bilgileri
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IFormFile? Logo { get; init; }
    #endregion

    #region Ayarlar (Settings)
    public string? WebhookUrl { get; init; }
    public BillingConfig? BillingConfig { get; init; }
    public SmtpConfiguration? SmtpConfig { get; init; }
    #endregion
}