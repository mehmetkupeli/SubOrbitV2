using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubOrbitV2.Domain.Entities.Billing;

namespace SubOrbitV2.Infrastructure.Persistence.Configurations.Billing;

public class PayerConfiguration : IEntityTypeConfiguration<Payer>
{
    public void Configure(EntityTypeBuilder<Payer> builder)
    {
        // PostgreSQL'de tablo isimleri genelde küçük harf (snake_case) tercih edilir 
        // ama EF Core default olarak PascalCase de yönetebilir. 
        // Standart kalalım:
        builder.ToTable("Payers");

        builder.HasKey(x => x.Id);

        // --- Multi-Tenancy & Index ---
        // PostgreSQL Index'i
        builder.HasIndex(x => new { x.ProjectId, x.ExternalId })
               .IsUnique();

        builder.HasIndex(x => x.NexiCustomerId);

        // --- Alan Ayarları ---
        builder.Property(x => x.ExternalId)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(x => x.Email)
               .IsRequired()
               .HasMaxLength(200);

        // Adres Bilgileri
        builder.Property(x => x.BillingAddress).HasMaxLength(500);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.TaxOffice).HasMaxLength(100);
        builder.Property(x => x.TaxNumber).HasMaxLength(50);

        // --- Finansal Ayarlar ---
        builder.Property(x => x.Currency)
               .IsRequired()
               .HasMaxLength(3)
               .HasDefaultValue("DKK");

        // KRİTİK: PostgreSQL'de "numeric(18,2)" üretir.
        builder.Property(x => x.VirtualBalance)
               .HasPrecision(18, 2)
               .HasDefaultValue(0);

        // --- Hizalama (Alignment) ---
        // Enum'lar PostgreSQL'de integer olarak tutulur (Default).
        builder.Property(x => x.AlignmentStrategy).IsRequired();
        builder.Property(x => x.BillingAnchorDay).IsRequired();

        // --- İlişkiler ---
        builder.HasMany(x => x.Subscriptions)
               .WithOne() // Subscriptions tablosunda Payer nesnesi tanımlı değilse boş bırakılır
               .HasForeignKey(s => s.PayerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Invoices)
               .WithOne()
               .HasForeignKey(i => i.PayerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}