using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Infrastructure.Data;
using SubOrbitV2.Infrastructure.Persistence.Repositories;
using SubOrbitV2.Infrastructure.Services.BackgroundJobs;
using SubOrbitV2.Infrastructure.Services.Billing;
using SubOrbitV2.Infrastructure.Services.Email;
using SubOrbitV2.Infrastructure.Services.Files;
using SubOrbitV2.Infrastructure.Services.Identity;
using SubOrbitV2.Infrastructure.Services.Integration;
using SubOrbitV2.Infrastructure.Services.Notification;
using SubOrbitV2.Infrastructure.Services.Payment.Nexi;
using SubOrbitV2.Infrastructure.Services.Pdf;
using SubOrbitV2.Infrastructure.Services.Security;
using System.Text;

namespace SubOrbitV2.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        #region 1. Database & Persistence (Veritabanı)

        // PostgreSQL Bağlantısı
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Context Interface Kaydı (Dependency Injection için)
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Generic Repository ve Unit of Work
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        #endregion

        #region 2. Identity & Context (Kimlik ve Oturum)

        // HttpContext Erişimi (CurrentUserService için şart)
        services.AddHttpContextAccessor();

        // Şu anki kullanıcıyı ve projeyi tanıyan servisler
        services.AddTransient<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IProjectContext, ProjectContext>();

        // JWT Ayarları ve Token Üretici
        // 1. JWT Ayarlarını Oku
        var jwtSettings = new JwtSettings();
        configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // 2. JWT Bearer Kimlik Doğrulamayı Kaydet (HATAYI BURASI ÇÖZER)
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
            };
        });
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Şifre Hashleme
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        #endregion

        #region 3. Security & Encryption (Güvenlik)

        services.Configure<SecuritySettings>(configuration.GetSection(SecuritySettings.SectionName));
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        #endregion

        #region 4. File & Media Services (Dosya İşlemleri)

        // Resim/Dosya Yükleme Servisi
        services.AddScoped<IFileService, LocalFileService>();

        // PDF Oluşturma Servisi
        services.AddTransient<IPdfService, QuestPdfService>();

        #endregion

        #region 5. Communication (E-posta ve Bildirim)

        // E-posta Ayarları ve Servisi
        services.Configure<SmtpConfiguration>(configuration.GetSection("SmtpConfiguration"));
        services.AddTransient<IEmailSender, SmtpEmailSender>();

        // Bildirim Servisi
        services.AddTransient<INotificationService, NotificationService>();
        services.AddTransient<INotificationDispatcherService, NotificationDispatcherService>();
        #endregion

        #region 6. Payment Gateway (Nexi Entegrasyonu)

        // Nexi Ayarları (appsettings.json'dan okur)
        services.Configure<NexiSettings>(configuration.GetSection(NexiSettings.SectionName));

        // HttpClient ile Nexi Servisini Ayağa Kaldırma
        services.AddHttpClient<INexiClient, NexiClient>();

        #endregion

        #region 7. Background Jobs (Hangfire)
        services.AddTransient<IMasterBillingJob, MasterBillingJob>();
        services.AddTransient<IProjectBillingWorkerJob, ProjectBillingWorkerJob>();
        services.AddTransient<INexiBulkDispatcherJob, NexiBulkDispatcherJob>();
        services.AddTransient<INexiStatusCheckerJob, NexiStatusCheckerJob>();

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(connectionString);
            }, new PostgreSqlStorageOptions
            {
                SchemaName = "hangfire",
                PrepareSchemaIfNecessary = true
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5;
        });

        #endregion

        #region 8. Integration & Webhooks (YENİ)
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddHttpClient<IWebhookDispatcherService, WebhookDispatcherService>();
        #endregion

        #region 9. Hesaplama Araçları
        services.AddSingleton<IPricingCalculatorService, PricingCalculatorService>();
        #endregion
        return services;
    }
}