using MediatR;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Organization.Queries.GetTenantProjects;

public record GetTenantProjectsQuery : IRequest<PaginatedResult<TenantProjectListItemDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}