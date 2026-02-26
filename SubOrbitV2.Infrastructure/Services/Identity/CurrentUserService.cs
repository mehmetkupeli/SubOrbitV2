using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SubOrbitV2.Application.Common.Interfaces;

namespace SubOrbitV2.Infrastructure.Services.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier); // veya "sub"
            if (idClaim != null && Guid.TryParse(idClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }
    }

    public Guid? ProjectId
    {
        get
        {
            // 1. Önce Token'daki "ProjectId" claim'ine bak (Payer için)
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("ProjectId");
            if (claim != null && Guid.TryParse(claim.Value, out var projectId))
            {
                return projectId;
            }

            // 2. Token'da yoksa Header'a bak "X-Project-Id" (Admin/Tenant için)
            if (_httpContextAccessor.HttpContext != null &&
                _httpContextAccessor.HttpContext.Request.Headers.TryGetValue("X-Project-Id", out var headerVal))
            {
                if (Guid.TryParse(headerVal.ToString(), out var headerProjectId))
                {
                    return headerProjectId;
                }
            }

            return null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid? TenantId
    {
        get
        {
            // 1. Önce Token'daki "ProjectId" claim'ine bak (Payer için)
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId");
            if (claim != null && Guid.TryParse(claim.Value, out var tenantId))
            {
                return tenantId;
            }

            // 2. Token'da yoksa Header'a bak "TenantId" (Admin/Tenant için)
            if (_httpContextAccessor.HttpContext != null &&
                _httpContextAccessor.HttpContext.Request.Headers.TryGetValue("TenantId", out var headerVal))
            {
                if (Guid.TryParse(headerVal.ToString(), out var tenantIdValue))
                {
                    return tenantIdValue;
                }
            }

            return null;
        }
    }
}