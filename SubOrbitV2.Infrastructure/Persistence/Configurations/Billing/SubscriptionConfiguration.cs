using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Catalog;

namespace SubOrbitV2.Infrastructure.Data.Configurations.Billing;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(x => x.Id);

        // ---------------------------
        // Indexler
        // ---------------------------
        builder.HasIndex(x => x.PayerId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => new { x.PayerId, x.IsMain });

        // ---------------------------
        // Property Ayarları
        // ---------------------------
        builder.Property(x => x.ExternalId)
               .HasMaxLength(100);

        builder.Property(x => x.Label)
               .HasMaxLength(200);

        builder.Property(x => x.VirtualBalance)
               .HasPrecision(18, 2)
               .HasDefaultValue(0);

        builder.Property(x => x.Quantity)
               .HasDefaultValue(1);

        builder.Property(x => x.Status)
               .HasConversion<int>();

        // ---------------------------
        // RELATIONSHIPS
        // ---------------------------

        // Payer (Required)
        builder.HasOne(x => x.Payer)
               .WithMany(p => p.Subscriptions)
               .HasForeignKey(x => x.PayerId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired();

        // Price (Required)
        builder.HasOne(x => x.Price)
               .WithMany() // Price tarafında collection yok
               .HasForeignKey(x => x.PriceId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired();

        // ActiveCoupon (Optional)
        builder.HasOne(x => x.ActiveCoupon)
               .WithMany()
               .HasForeignKey(x => x.ActiveCouponId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        // Product (Required - navigation yok ama FK var)
        builder.HasOne<Product>()
               .WithMany()
               .HasForeignKey(x => x.ProductId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired();
    }
}