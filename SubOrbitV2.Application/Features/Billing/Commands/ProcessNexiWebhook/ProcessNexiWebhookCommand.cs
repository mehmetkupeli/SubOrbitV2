using MediatR;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Billing.Commands.ProcessNexiWebhook;

public record ProcessNexiWebhookCommand(Guid ProjectId, string SubId, string AuthorizationToken) : IRequest<Result<bool>>;