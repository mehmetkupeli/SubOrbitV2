using Microsoft.EntityFrameworkCore;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Entities.Communication;
using SubOrbitV2.Domain.Entities.Identity;
using SubOrbitV2.Domain.Entities.Integration;
using SubOrbitV2.Domain.Entities.Organization;

namespace SubOrbitV2.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Organization
    DbSet<Tenant> Tenants { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectSetting> ProjectSettings { get; }

    // Identity
    DbSet<AppUser> AppUsers { get; }
    DbSet<PortalToken> PortalTokens { get; }

    // Catalog & Products
    DbSet<CatalogCategory> CatalogCategories { get; }
    DbSet<Product> Products { get; }
    DbSet<Price> Prices { get; }
    DbSet<FeatureGroup> FeatureGroups { get; }
    DbSet<Feature> Features { get; }
    DbSet<ProductFeatureValue> ProductFeatureValues { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<CouponRedemption> CouponRedemptions { get; }

    // Billing
    DbSet<Payer> Payers { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLine> InvoiceLines { get; }
    DbSet<WalletTransaction> WalletTransactions { get; }
    DbSet<BulkOperation> BulkOperations { get; }
    DbSet<SubscriptionActivityLog> SubscriptionLogs { get; }

    // Communication & Integration
    DbSet<NotificationQueue> NotificationQueues { get; }
    DbSet<WebhookEvent> WebhookEvents { get; }
    DbSet<WebhookDeliveryAttempt> WebhookDeliveryAttempts { get; }

    // Save Method
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}