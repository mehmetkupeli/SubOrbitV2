using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubOrbitV2.Domain.Entities.Billing;

namespace SubOrbitV2.Infrastructure.Persistence.Configurations.Billing;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(x => x.Id);

        // --- Indexler ---
        builder.HasIndex(x => x.PayerId);
        builder.HasIndex(x => x.ProductId);
        // Composite Index
        builder.HasIndex(x => new { x.PayerId, x.IsMain });

        // --- Alan Ayarları ---
        builder.Property(x => x.ExternalId).HasMaxLength(100);
        builder.Property(x => x.Label).HasMaxLength(200);

        // --- Finansal ---
        builder.Property(x => x.VirtualBalance)
               .HasPrecision(18, 2)
               .HasDefaultValue(0);

        builder.Property(x => x.Quantity)
               .HasDefaultValue(1);

        // --- İlişkiler ---
        builder.HasOne<Payer>()
               .WithMany(p => p.Subscriptions)
               .HasForeignKey(x => x.PayerId)
               .IsRequired();

        // Product & Price ilişkileri (Restrict)
        builder.HasOne<Domain.Entities.Catalog.Product>()
               .WithMany()
               .HasForeignKey(x => x.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Entities.Catalog.Price>()
               .WithMany()
               .HasForeignKey(x => x.PriceId)
               .OnDelete(DeleteBehavior.Restrict);

        // Kupon (SetNull)
        builder.HasOne<Domain.Entities.Catalog.Coupon>()
               .WithMany()
               .HasForeignKey(x => x.ActiveCouponId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);
    }
}