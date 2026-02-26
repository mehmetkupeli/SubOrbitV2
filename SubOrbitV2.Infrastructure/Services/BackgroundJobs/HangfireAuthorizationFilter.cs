using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace SubOrbitV2.Infrastructure.Services.BackgroundJobs;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // 1. Geliştirme ortamında (Localhost) her zaman izin ver.
        // Bu sayede login sistemi bitmeden de test edebilirsin.
        if (IsLocal(httpContext.Connection))
        {
            return true;
        }

        // 2. Canlı ortamda sadece Authenticated (Giriş yapmış) ve Admin olanlara izin ver.
        // NOT: Identity sistemi (Faz 4) devreye girdiğinde burası çalışacak.
        var user = httpContext.User;
        if (user != null && user.Identity != null && user.Identity.IsAuthenticated)
        {
            // İleride buraya "&& user.IsInRole("Admin")" ekleyeceğiz.
            return true;
        }

        return false;
    }

    private static bool IsLocal(ConnectionInfo connection)
    {
        if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
        {
            return true;
        }

        if (connection.RemoteIpAddress != null && connection.LocalIpAddress != null)
        {
            return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
        }

        return System.Net.IPAddress.IsLoopback(connection.RemoteIpAddress!);
    }
}