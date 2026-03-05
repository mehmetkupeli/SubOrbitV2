using MediatR;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Billing.Commands.ProcessRecurringBilling;
public record ProcessProjectRecurringBillingCommand(Guid ProjectId) : IRequest<Result<bool>>;