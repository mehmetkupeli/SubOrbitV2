using MediatR;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateFeatureGroup;

public record CreateFeatureGroupCommand : IRequest<Result<CreateFeatureGroupResponse>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}