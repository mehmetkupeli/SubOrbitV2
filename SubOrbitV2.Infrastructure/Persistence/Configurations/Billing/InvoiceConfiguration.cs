using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubOrbitV2.Domain.Entities.Billing;

namespace SubOrbitV2.Infrastructure.Persistence.Configurations.Billing;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(x => x.Id);

        // --- Indexler ---
        // Her proje içinde Fatura Numarası benzersiz olmalı (INV-2026-001).
        builder.HasIndex(x => new { x.ProjectId, x.Number })
               .IsUnique();

        // Raporlama ve filtreleme için hızlı erişim
        builder.HasIndex(x => x.PayerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => x.NexiTransactionId); // Bankadan sorgulama için

        // --- Alan Ayarları ---
        builder.Property(x => x.Number)
               .IsRequired()
               .HasMaxLength(50);

        // Snapshot Alanları (Müşteri o anki adı/adresi)
        builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CustomerEmail).HasMaxLength(200);
        builder.Property(x => x.CustomerTaxOffice).HasMaxLength(100);
        builder.Property(x => x.CustomerTaxNumber).HasMaxLength(50);
        builder.Property(x => x.CustomerAddress).HasMaxLength(500);
        builder.Property(x => x.CustomerCity).HasMaxLength(100);
        builder.Property(x => x.CustomerCountry).HasMaxLength(100);

        builder.Property(x => x.PdfPath).HasMaxLength(500);
        builder.Property(x => x.HostedInvoiceUrl).HasMaxLength(500);
        builder.Property(x => x.NexiTransactionId).HasMaxLength(100);

        builder.Property(x => x.Currency)
               .IsRequired()
               .HasMaxLength(3)
               .HasDefaultValue("DKK");

        // --- Finansal Hassasiyet (PostgreSQL numeric) ---
        // Tüm para alanları (18, 2) olmalı.
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.TotalDiscount).HasPrecision(18, 2);
        builder.Property(x => x.TotalTax).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.AmountCredited).HasPrecision(18, 2);
        builder.Property(x => x.AmountPaid).HasPrecision(18, 2);
        builder.Property(x => x.AmountRemaining).HasPrecision(18, 2);

        // --- İlişkiler ---

        // Payer (Zorunlu) -> Ama Payer silinirse Fatura silinmesin (Restrict)!
        // Maliye ile başımız belaya girmesin.
        builder.HasOne<Domain.Entities.Billing.Payer>()
               .WithMany(p => p.Invoices)
               .HasForeignKey(x => x.PayerId)
               .OnDelete(DeleteBehavior.Restrict);

        // Satırlar (Lines)
        // Fatura başlığı silinirse satırları da silinsin (Cascade).
        // Çünkü satırların tek başına bir anlamı yok.
        builder.HasMany(x => x.Lines) // Invoice entity'sine bu koleksiyonu eklememiz lazım (aş. bak)
               .WithOne() // InvoiceLine içinde Invoice navigation varsa belirt, yoksa boş.
               .HasForeignKey(l => l.InvoiceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}