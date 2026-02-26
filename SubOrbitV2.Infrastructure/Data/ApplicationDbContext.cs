using MediatR;
using Microsoft.EntityFrameworkCore;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Entities.Communication;
using SubOrbitV2.Domain.Entities.Identity;
using SubOrbitV2.Domain.Entities.Integration;
using SubOrbitV2.Domain.Entities.Organization;
using System.Linq.Expressions;
using System.Reflection;

namespace SubOrbitV2.Infrastructure.Data;

/// <summary>
/// Uygulamanın ana veritabanı bağlamı (Context).
/// Entity Framework Core üzerinden veritabanı işlemlerini yönetir.
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    #region Private Fields

    private readonly IPublisher _publisher;
    private readonly IProjectContext _projectContext;
    private readonly ICurrentUserService _currentUserService;

    #endregion

    #region Constructor & Configuration

    static ApplicationDbContext()
    {
        // PostgreSQL UTC Timestamp sorununu çözer.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IProjectContext projectContext,
        ICurrentUserService currentUserService,
        IPublisher publisher) : base(options)
    {
        _publisher = publisher;
        _projectContext = projectContext;
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region 1. PostgreSQL UTC Fix
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));

            foreach (var property in properties)
            {
                modelBuilder.Entity(entityType.ClrType).Property(property.Name).HasColumnType("timestamp with time zone");
            }
        }
        #endregion

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        #region 2. Global Query Filter (SaaS Isolation)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // IMustHaveProject arayüzünü implemente eden tabloları bul
            if (typeof(IMustHaveProject).IsAssignableFrom(entityType.ClrType))
            {
                // Metodu bul
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(ConvertFilterExpression), BindingFlags.NonPublic | BindingFlags.Instance);

                if (method != null)
                {
                    // DİKKAT: Metot generic olmadığı için MakeGenericMethod ÇAĞIRMIYORUZ.
                    // Sadece parametre olarak Type gönderiyoruz.
                    var filter = method.Invoke(this, new object[] { entityType.ClrType });
                    entityType.SetQueryFilter((LambdaExpression)filter!);
                }
            }
        }
        #endregion

        base.OnModelCreating(modelBuilder);
    }

    #endregion

    #region DbSets (Tablolar)

    // Organization
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectSetting> ProjectSettings { get; set; }

    // Identity
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<PortalToken> PortalTokens { get; set; }

    // Catalog & Products
    public DbSet<CatalogCategory> CatalogCategories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Price> Prices { get; set; }
    public DbSet<FeatureGroup> FeatureGroups { get; set; }
    public DbSet<Feature> Features { get; set; }
    public DbSet<ProductFeatureValue> ProductFeatureValues { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<CouponRedemption> CouponRedemptions { get; set; }

    // Billing (Unified Billing Core)
    public DbSet<Payer> Payers { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLine> InvoiceLines { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }
    public DbSet<BulkOperation> BulkOperations { get; set; }
    public DbSet<SubscriptionActivityLog> SubscriptionLogs { get; set; }

    // Communication & Integration
    public DbSet<NotificationQueue> NotificationQueues { get; set; }
    public DbSet<WebhookEvent> WebhookEvents { get; set; }
    public DbSet<WebhookDeliveryAttempt> WebhookDeliveryAttempts { get; set; }

    #endregion

    #region SaveChanges & Event Dispatching

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Audit ve SaaS Alanlarını Doldur
        ProcessAuditAndTenantFields();

        // 2. Veritabanına Kaydet
        var result = await base.SaveChangesAsync(cancellationToken);

        // 3. Domain Event'leri Fırlat (Dispatcher)
        await DispatchDomainEventsAsync();

        return result;
    }

    private void ProcessAuditAndTenantFields()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    if (_currentUserService.UserId != null)
                    {
                        entry.Entity.CreatedBy = _currentUserService.UserId; 
                    }
                    if (entry.Entity is IMustHaveProject projectEntity && projectEntity.ProjectId == Guid.Empty)
                    {
                        if (_projectContext.IsIdSet)
                        {
                            projectEntity.ProjectId = _projectContext.ProjectId;
                        }
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    if (_currentUserService.UserId != null)
                    {
                        entry.Entity.LastModifiedBy = _currentUserService.UserId;
                    }
                    break;
            }
        }
    }

    private async Task DispatchDomainEventsAsync()
    {
        var entitiesWithEvents = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        if (!entitiesWithEvents.Any())
            return;

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent);
        }
    }
    public Guid CurrentProjectId => _projectContext.ProjectId;

    // Global Query Filter için Expression Oluşturucu
    private LambdaExpression ConvertFilterExpression(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(IMustHaveProject.ProjectId));

        // 'this' (DbContext) üzerinden CurrentProjectId property'sine erişen expression
        var dbContextParameter = Expression.Constant(this);
        var currentProjectIdProperty = Expression.Property(dbContextParameter, nameof(CurrentProjectId));

        // e.ProjectId == this.CurrentProjectId
        var comparison = Expression.Equal(property, currentProjectIdProperty);

        return Expression.Lambda(comparison, parameter);
    }

    #endregion
}