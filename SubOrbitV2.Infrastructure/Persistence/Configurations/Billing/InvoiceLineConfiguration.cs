using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubOrbitV2.Domain.Entities.Billing;

namespace SubOrbitV2.Infrastructure.Persistence.Configurations.Billing;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");

        builder.HasKey(x => x.Id);

        // --- Index ---
        builder.HasIndex(x => x.InvoiceId);
        builder.HasIndex(x => x.SubscriptionId);

        // --- Alan Ayarları ---
        builder.Property(x => x.Description)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(x => x.Quantity)
               .IsRequired()
               .HasDefaultValue(1);

        // --- Finansal Hassasiyet ---
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

        // Vergi Oranı (%25.50 gibi olabilir)
        builder.Property(x => x.TaxRate).HasPrecision(18, 2);

        // --- İlişkiler ---

        // Invoice (Zorunlu)
        builder.HasOne<Invoice>() // Eğer InvoiceLine'da navigation varsa belirt
               .WithMany(i => i.Lines)
               .HasForeignKey(x => x.InvoiceId)
               .IsRequired();

        // Subscription (Opsiyonel)
        // Satır bir aboneliğe bağlı olabilir ama abonelik silinse bile
        // fatura satırı kalmalı (SetNull).
        builder.HasOne<Domain.Entities.Billing.Subscription>()
               .WithMany()
               .HasForeignKey(x => x.SubscriptionId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        // Product (Opsiyonel - Raporlama için)
        builder.HasOne<Domain.Entities.Catalog.Product>()
               .WithMany()
               .HasForeignKey(x => x.ProductId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);
    }
}