using Microsoft.EntityFrameworkCore;
using SubOrbitV2.Application.Common.Attributes;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Infrastructure.Data;

namespace SubOrbitV2.Api.Middleware;

public class ProjectResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public ProjectResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IProjectContext projectContext, ApplicationDbContext dbContext)
    {
        // 1. Statik Dosyalar ve Swagger muafiyeti (Bunlar endpoint bile değil)
        var path = context.Request.Path.Value?.ToLower();
        if (path != null && (path.StartsWith("/swagger") || path.StartsWith("/favicon.ico")))
        {
            await _next(context);
            return;
        }

        // 2. Mevcut Endpoint'i al
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        // 3. [AllowAnonymous] kontrolü (Login gerektirmeyenler her zaman geçer)
        if (endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
        {
            await _next(context);
            return;
        }

        // 4. [MustHaveProject] Kontrolü (İŞTE AKILLI KISIM BURASI)
        // Eğer endpoint'te bu attribute YOKSA, ProjectId zorunlu değildir.
        var mustHaveProject = endpoint.Metadata.GetMetadata<MustHaveProjectAttribute>();
        if (mustHaveProject == null)
        {
            await _next(context);
            return;
        }

        // 5. Eğer buraya geldiysek istekte ProjectId ZORUNLUDUR!
        Guid? resolvedProjectId = null;

        // Senaryo A: Payer (Müşteri Portalı) girişi - Token'dan oku
        var projectIdClaim = context.User.FindFirst("ProjectId")?.Value;
        if (!string.IsNullOrEmpty(projectIdClaim) && Guid.TryParse(projectIdClaim, out var tokenProjectId))
        {
            resolvedProjectId = tokenProjectId;
        }

        // Senaryo B: Tenant/Admin girişi - Header'dan oku
        if (resolvedProjectId == null && context.Request.Headers.TryGetValue("X-Project-Id", out var headerValue))
        {
            if (Guid.TryParse(headerValue.ToString(), out var headerProjectId))
            {
                resolvedProjectId = headerProjectId;
            }
        }

        // 6. ID hala yoksa: REDDET!
        if (resolvedProjectId == null || resolvedProjectId == Guid.Empty)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { Error = "Bu işlem için 'X-Project-Id' header'ı zorunludur." });
            return;
        }

        if (resolvedProjectId.HasValue)
        {
            var project = await dbContext.Projects.AsNoTracking().Include(p => p.Settings).FirstOrDefaultAsync(p => p.Id == resolvedProjectId.Value);
            if (project != null)
                projectContext.SetProject(project);

        }

        // 7. Her şey okeyse Context'i doldur ve devam et
        projectContext.SetProjectId(resolvedProjectId.Value);
        await _next(context);
    }
}