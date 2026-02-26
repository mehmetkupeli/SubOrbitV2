using MediatR;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateFeature;

public record CreateFeatureCommand : IRequest<Result<CreateFeatureResponse>>
{
    public Guid FeatureGroupId { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public FeatureDataType DataType { get; init; }
}