using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Organization;

namespace SubOrbitV2.Infrastructure.Services.Pdf;

public class QuestPdfService : IPdfService
{
    private readonly IWebHostEnvironment _env;

    public QuestPdfService(IWebHostEnvironment env)
    {
        _env = env;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, Project project)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);

                // Varsayılan font ayarları (Daha modern ve temiz)
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor(Colors.Grey.Darken3));

                page.Header().Element(handler => ComposeHeader(handler, invoice, project));
                page.Content().Element(handler => ComposeContent(handler, invoice));
                page.Footer().Element(ComposeFooter);
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    #region Design Components

    private void ComposeHeader(IContainer container, Invoice invoice, Project project)
    {
        container.Row(row =>
        {
            // SOL ÜST: Proje Marka Kimliği (Billed From)
            row.RelativeItem().Column(column =>
            {
                // Projenin logosu varsa, fiziksel yolunu bulup PDF'e basıyoruz
                if (!string.IsNullOrEmpty(project.LogoUrl))
                {
                    var logoPath = Path.Combine(_env.ContentRootPath, project.LogoUrl.TrimStart('/'));
                    if (File.Exists(logoPath))
                    {
                        column.Item().Height(40).Image(logoPath);
                        column.Item().PaddingTop(5);
                    }
                }

                column.Item().Text(project.Name).FontSize(20).Bold().FontColor(Colors.Grey.Darken4);
                column.Item().Text("Billed From").FontSize(9).FontColor(Colors.Grey.Medium);
            });

            // SAĞ ÜST: Fatura Detayları ve Durum Rozeti
            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text("INVOICE / FATURA").FontSize(24).Black().FontColor(Colors.Blue.Darken2);

                column.Item().PaddingTop(5).Text(text =>
                {
                    text.Span("Fatura No: ").SemiBold();
                    text.Span(invoice.Number);
                });

                column.Item().Text(text =>
                {
                    text.Span("Düzenlenme: ").SemiBold();
                    text.Span($"{invoice.CreatedAt:dd.MM.yyyy}");
                });

                if (invoice.DueDate.HasValue)
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Son Ödeme: ").SemiBold();
                        text.Span($"{invoice.DueDate.Value:dd.MM.yyyy}");
                    });
                }
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice)
    {
        container.PaddingVertical(30).Column(column =>
        {
            // ALICI BİLGİLERİ (Billed To)
            column.Item().PaddingBottom(20).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    // DÜZELTME: PaddingBottom(5) Text'ten ÖNCEYE alındı! (CS1929 Hatası Çözümü)
                    col.Item().PaddingBottom(5).Text("Faturalandırılan (Billed To):").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);

                    col.Item().Text(invoice.CustomerName).Bold().FontSize(12);

                    // E-Posta
                    if (!string.IsNullOrEmpty(invoice.CustomerEmail))
                        col.Item().Text(text =>
                        {
                            text.Span("E-Posta: ").SemiBold().FontColor(Colors.Grey.Darken2);
                            text.Span(invoice.CustomerEmail);
                        });

                    // Adres
                    if (!string.IsNullOrEmpty(invoice.CustomerAddress))
                        col.Item().Text(text =>
                        {
                            text.Span("Adres: ").SemiBold().FontColor(Colors.Grey.Darken2);
                            text.Span(invoice.CustomerAddress);
                        });

                    // Şehir / Ülke
                    if (!string.IsNullOrEmpty(invoice.CustomerCity) || !string.IsNullOrEmpty(invoice.CustomerCountry))
                        col.Item().Text(text =>
                        {
                            text.Span("Bölge: ").SemiBold().FontColor(Colors.Grey.Darken2);
                            text.Span($"{invoice.CustomerCity} / {invoice.CustomerCountry}");
                        });

                    // Vergi Numarası
                    if (!string.IsNullOrEmpty(invoice.CustomerTaxNumber))
                        col.Item().PaddingTop(3).Text(text =>
                        {
                            text.Span("VKN/TCKN: ").SemiBold().FontColor(Colors.Grey.Darken3);
                            text.Span(invoice.CustomerTaxNumber);
                        });
                });
            });

            // FATURA KALEMLERİ (Minimalist Modern Tablo)
            column.Item().Element(handler => ComposeTable(handler, invoice));

            // ALT TOPLAMLAR (Sağ Alt Köşe)
            var totalPriceArea = column.Item().PaddingTop(20).AlignRight().Width(250);
            totalPriceArea.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("Ara Toplam:");
                    row.RelativeItem().AlignRight().Text($"{invoice.Subtotal:N2} {invoice.Currency}");
                });

                if (invoice.TotalDiscount > 0)
                {
                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem().Text("İndirim:").FontColor(Colors.Red.Medium);
                        row.RelativeItem().AlignRight().Text($"-{invoice.TotalDiscount:N2} {invoice.Currency}").FontColor(Colors.Red.Medium);
                    });
                }

                if (invoice.TotalTax > 0)
                {
                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem().Text("Vergi:");
                        row.RelativeItem().AlignRight().Text($"{invoice.TotalTax:N2} {invoice.Currency}");
                    });
                }

                // GENEL TOPLAM Blok Tasarımı (Vurgulu)
                col.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Text("GENEL TOPLAM:").Bold().FontSize(11);
                    row.RelativeItem().AlignRight().Text($"{invoice.TotalAmount:N2} {invoice.Currency}").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                });
            });
        });
    }

    private void ComposeTable(IContainer container, Invoice invoice)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30); // #
                columns.RelativeColumn(4);  // Açıklama
                columns.RelativeColumn(2);  // Birim Fiyat
                columns.RelativeColumn(1);  // Adet
                columns.RelativeColumn(2);  // Toplam
            });

            // Tablo Başlığı (Dikey çizgisiz, temiz)
            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("#");
                header.Cell().Element(HeaderStyle).Text("Hizmet / Açıklama");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Birim Fiyat");
                header.Cell().Element(HeaderStyle).AlignCenter().Text("Adet");
                header.Cell().Element(HeaderStyle).AlignRight().Text("Toplam");

                static IContainer HeaderStyle(IContainer container) =>
                    container.Background(Colors.Grey.Lighten4)
                             .PaddingVertical(8)
                             .PaddingHorizontal(5)
                             .BorderBottom(1)
                             .BorderColor(Colors.Grey.Lighten2)
                             .DefaultTextStyle(x => x.SemiBold().FontSize(9).FontColor(Colors.Grey.Darken2));
            });

            // Tablo İçeriği
            if (invoice.Lines != null)
            {
                foreach (var item in invoice.Lines.Select((value, i) => new { i, value }))
                {
                    table.Cell().Element(CellStyle).Text($"{item.i + 1}").FontSize(9);

                    // Açıklama ve "Kısmi Ödeme (Proration)" İtalik Notu
                    table.Cell().Element(CellStyle).Column(col =>
                    {
                        col.Item().Text(item.value.Description).SemiBold();
                        if (item.value.IsProration)
                        {
                            col.Item().Text("Bu kalem kısmi dönem (proration) için indirimli hesaplanmıştır.")
                               .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                        }
                    });

                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.value.UnitPrice:N2} {invoice.Currency}");
                    table.Cell().Element(CellStyle).AlignCenter().Text($"{item.value.Quantity}");
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.value.TotalAmount:N2} {invoice.Currency}").SemiBold();

                    static IContainer CellStyle(IContainer container) =>
                        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten4).PaddingVertical(10).PaddingHorizontal(5);
                }
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(10).AlignCenter().Text("Bizi tercih ettiğiniz için teşekkür ederiz.")
                .FontSize(10).SemiBold().FontColor(Colors.Grey.Darken1);

            column.Item().Row(row =>
            {
                // DÜZELTME: Stil (DefaultTextStyle) Text bloğundan ÖNCEYE alındı! (CS0023 Hatası Çözümü)
                row.RelativeItem().DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium)).Text(x =>
                {
                    x.Span("Sayfa ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });

                // İşte O Gizli ve Şık Reklamımız
                row.RelativeItem().AlignRight().Text("⚡ Powered by SubOrbit V2 Billing Infrastructure")
                    .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
            });
        });
    }
    #endregion
}