using MediatR;
using Microsoft.EntityFrameworkCore;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Organization.Queries.GetTenantProjects;

public class GetTenantProjectsQueryHandler : IRequestHandler<GetTenantProjectsQuery, PaginatedResult<TenantProjectListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetTenantProjectsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedResult<TenantProjectListItemDto>> Handle(GetTenantProjectsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId;

        // 1. Sorguyu oluştur (Henüz DB'ye gitmedi)
        var query = _context.Projects
            .AsNoTracking() 
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt);

        // 2. Toplam sayıyı al
        var count = await query.CountAsync(cancellationToken);

        // 3. Sayfalanmış veriyi çek ve DTO'ya map et
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new TenantProjectListItemDto(
                p.Id,
                p.Name,
                p.Description,
                p.LogoUrl,
                p.ApiKey,
                p.IsActive,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        // 4. PaginatedResult ile sarmala ve dön
        return PaginatedResult<TenantProjectListItemDto>.Create(items, count, request.PageNumber, request.PageSize);
    }
}