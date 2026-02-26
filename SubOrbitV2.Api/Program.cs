using Hangfire;
using SubOrbitV2.Api.Middleware;
using SubOrbitV2.Application;
using SubOrbitV2.Infrastructure;
using SubOrbitV2.Infrastructure.Services.BackgroundJobs;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

#region 1. Configuration & Secrets (Konfigürasyonlar)
// -----------------------------------------------------------------------------
// Bu bölgede appsettings.json, Environment Variables ve Secret Manager yüklenir.
// Veritabaný baðlantý cümleciði (Connection String) buradan okunur.
// -----------------------------------------------------------------------------

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
#endregion

#region 2. Service Registration (Dependency Injection Container)
// -----------------------------------------------------------------------------
// Servislerin (Interface ve Class eþleþmeleri) ve DbContext'in eklendiði alan.
// Transient, Scoped, Singleton tanýmlarý burada yapýlýr.
// -----------------------------------------------------------------------------

// 2.2 API Controllers
// JSON serileþtirme ayarlarý gerekirse .AddJsonOptions() ile buraya eklenir.
builder.Services.AddControllers();

// 2.3 Swagger / OpenAPI Generator
// API dokümantasyonu için gerekli servisler.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SubOrbit V2 API", Version = "v1" });

    #region JWT Auth Definition (Mevcut olan)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    #endregion

    #region X-Project-Id Header Definition (YENÝ)
    // Bu kýsým sayesinde Swagger'a bir 'anahtar' daha ekliyoruz
    c.AddSecurityDefinition("ProjectId", new OpenApiSecurityScheme
    {
        Name = "X-Project-Id", // Header anahtar ismi
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Ýþlem yapýlacak Proje ID'sini (Guid) giriniz."
    });
    #endregion

    #region Security Requirement
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        // 1. Bearer Token Gereksinimi
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        },
        // 2. Project ID Gereksinimi (YENÝ)
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ProjectId" }
            },
            Array.Empty<string>()
        }
    });
    #endregion
});
// 2.4 Application Services (IoC)
builder.Services.AddApplicationServices();

#endregion

var app = builder.Build();

#region 3. HTTP Request Pipeline (Middleware)
// -----------------------------------------------------------------------------
// Gelen isteðin (Request) nasýl iþleneceðini belirleyen boru hattý.
// SIRALAMA ÇOK KRÝTÝKTÝR! (Önce Exception, sonra Https, sonra Auth...)
// -----------------------------------------------------------------------------

// 3.1 Global Exception Handler 
app.UseMiddleware<ExceptionMiddleware>();

// 3.2 Swagger UI (Sadece Geliþtirme Ortamýnda)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SubOrbit V2 API v1");
        c.RoutePrefix = string.Empty; // API açýlýnca direkt Swagger gelsin (isteðe baðlý)
    });
}

// 3.3 Security Headers & HTTPS
app.UseHttpsRedirection();

// 3.4 Static Files (Fatura PDF'leri vb. sunmak için)
app.UseStaticFiles();

// 3.5 Routing & CORS
app.UseRouting();

// CORS ayarlarý buraya gelecek (React/Vue frontend için)
// app.UseCors("AllowAll");

// 3.6 Authentication & Authorization
// Önce kimlik doðrula (Kimsin?), sonra yetki ver (Girebilir misin?).
app.UseAuthentication(); // Ýleride JWT eklenecek
app.UseMiddleware<ProjectResolutionMiddleware>();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "SubOrbit V2 Jobs"
});

// 3.7 Endpoint Mapping
app.MapControllers();

#endregion

#region 4. Application Startup (Uygulama Baþlatma)
// -----------------------------------------------------------------------------
// Uygulamanýn ayaða kalktýðý son nokta.
// -----------------------------------------------------------------------------

// Loglama: Uygulama baþladý
// Log.Information("SubOrbit V2 API Starting Up...");

app.Run();

#endregion