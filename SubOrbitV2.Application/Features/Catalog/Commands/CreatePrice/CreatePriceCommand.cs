using MediatR;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreatePrice;

public record CreatePriceCommand : IRequest<Result<CreatePriceResponse>>
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "DKK";
    public decimal VatRate { get; init; }
    public BillingInterval Interval { get; init; }
    public int IntervalCount { get; init; } = 1;
    public int TrialDays { get; init; } = 0;
    public bool IsActive { get; init; } = true;
}