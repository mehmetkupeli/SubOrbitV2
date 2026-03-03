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
        // 1. Statik Dosyalar ve Swagger muafiyeti
        var path = context.Request.Path.Value?.ToLower();
        if (path != null && (path.StartsWith("/swagger") || path.StartsWith("/favicon.ico")))
        {
            await _next(context);
            return;
        }

        #region 2. ÇÖZÜMLEME (RESOLUTION) AŞAMASI: ProjectId'yi her yerden ara
        Guid? resolvedProjectId = null;

        // Senaryo A: Payer (Müşteri Portalı) girişi - Token'dan (Claim) oku
        var projectIdClaim = context.User.FindFirst("ProjectId")?.Value;
        if (!string.IsNullOrEmpty(projectIdClaim) && Guid.TryParse(projectIdClaim, out var tokenProjectId))
        {
            resolvedProjectId = tokenProjectId;
        }

        // Senaryo B: Tenant/Admin girişi - Header'dan (X-Project-Id) oku
        if (resolvedProjectId == null && context.Request.Headers.TryGetValue("X-Project-Id", out var headerValue))
        {
            if (Guid.TryParse(headerValue.ToString(), out var headerProjectId))
            {
                resolvedProjectId = headerProjectId;
            }
        }

        // Senaryo C: Webhook girişi - QueryString'den (URL'den) oku (YENİ EKLENDİ)
        if (resolvedProjectId == null && context.Request.Query.TryGetValue("projectId", out var queryValue))
        {
            if (Guid.TryParse(queryValue.ToString(), out var queryProjectId))
            {
                resolvedProjectId = queryProjectId;
            }
        }
        #endregion

        #region 3. CONTEXT'İ DOLDURMA (Bağlamı Yarat)
        // Eğer ID bir şekilde bulunduysa, yetki (Auth) durumuna bakmaksızın Context'e yükle.
        if (resolvedProjectId.HasValue && resolvedProjectId.Value != Guid.Empty)
        {
            var project = await dbContext.Projects
                .AsNoTracking()
                .Include(p => p.Settings)
                .FirstOrDefaultAsync(p => p.Id == resolvedProjectId.Value);

            if (project != null)
            {
                projectContext.SetProject(project);
                projectContext.SetProjectId(resolvedProjectId.Value);
            }
        }
        #endregion

        #region 4. DENETİM (ENFORCEMENT) AŞAMASI
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var mustHaveProject = endpoint.Metadata.GetMetadata<MustHaveProjectAttribute>();

            // [MustHaveProject] zorunluluğu var ama biz 2. aşamada bir proje bulamadıysak İŞLEMİ REDDET!
            // Dikkat: [AllowAnonymous] olsa bile, eğer [MustHaveProject] konulmuşsa acımaz, reddeder.
            if (mustHaveProject != null && !projectContext.IsIdSet)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { Error = "Bu işlem için geçerli bir Proje (X-Project-Id veya URL Parametresi) zorunludur." });
                return;
            }
        }
        #endregion

        // 5. Her şey yolunda, isteği Handler/Controller'a aktar
        await _next(context);
    }
}