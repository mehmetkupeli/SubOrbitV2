namespace SubOrbitV2.Application.Features.Catalog.Commands.CreatePrice;

public record CreatePriceResponse(Guid Id, Guid ProductId, string Name, decimal Amount, string Currency);