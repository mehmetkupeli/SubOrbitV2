using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Catalog;

namespace SubOrbitV2.Infrastructure.Persistence.Configurations.Billing;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.InvoiceId);
        builder.HasIndex(x => x.SubscriptionId);

        builder.Property(x => x.Description)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(x => x.Quantity)
               .IsRequired()
               .HasDefaultValue(1);

        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxRate).HasPrecision(18, 2);

        // 🔥 BURASI KRİTİK
        builder.HasOne(x => x.Invoice)
               .WithMany(i => i.Lines)
               .HasForeignKey(x => x.InvoiceId)
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired();

        builder.HasOne<Subscription>()
               .WithMany()
               .HasForeignKey(x => x.SubscriptionId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        builder.HasOne<Product>()
               .WithMany()
               .HasForeignKey(x => x.ProductId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);
    }
}