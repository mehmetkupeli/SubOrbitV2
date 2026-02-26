namespace SubOrbitV2.Application.Features.Catalog.Commands.SetProductFeatureValue;

public record SetProductFeatureValueResponse(Guid Id, Guid ProductId, Guid FeatureId, string Value);