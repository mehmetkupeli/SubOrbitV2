using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Organization;

namespace SubOrbitV2.Infrastructure.Services.Pdf;

public class QuestPdfService : IPdfService
{
    public QuestPdfService()
    {
        // QuestPDF Community Lisansı
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
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Element(handler => ComposeHeader(handler, invoice,project));
                page.Content().Element(handler => ComposeContent(handler, invoice));
                page.Footer().Element(ComposeFooter);
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    #region Design Components

    private void ComposeHeader(IContainer container, Invoice invoice, Project project)
    {
        var titleStyle = TextStyle.Default.FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

        container.Row(row =>
        {
            // Sol: Fatura Bilgileri
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(project.Name).FontSize(24).Bold().FontColor(Colors.Grey.Darken3);
                column.Item().Text($"FATURA #{invoice.Number}").Style(titleStyle);

                column.Item().Text(text =>
                {
                    text.Span("Tarih: ").SemiBold();
                    text.Span($"{invoice.CreatedAt:dd.MM.yyyy}");
                });

                column.Item().Text(text =>
                {
                    text.Span("Vade: ").SemiBold();
                    // DueDate nullable olduğu için kontrol ediyoruz
                    text.Span(invoice.DueDate.HasValue ? $"{invoice.DueDate.Value:dd.MM.yyyy}" : "-");
                });

                column.Item().PaddingTop(5).Text(invoice.Status.ToString().ToUpper())
                    .FontColor(invoice.Status == Domain.Enums.InvoiceStatus.Paid ? Colors.Green.Medium : Colors.Red.Medium)
                    .Bold();
            });

            // Sağ: Müşteri Bilgileri (SNAPSHOT Verileri)
            // Payer ilişkisine gitmek yerine Fatura üzerindeki kopyaları kullanıyoruz.
            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text("SAYIN MÜŞTERİ,").SemiBold();

                // Entity: CustomerName
                column.Item().Text(invoice.CustomerName);

                // Entity: CustomerEmail
                if (!string.IsNullOrEmpty(invoice.CustomerEmail))
                    column.Item().Text(invoice.CustomerEmail);

                // Entity: CustomerAddress, City, Country
                if (!string.IsNullOrEmpty(invoice.CustomerAddress))
                    column.Item().Text(invoice.CustomerAddress);

                if (!string.IsNullOrEmpty(invoice.CustomerCity) || !string.IsNullOrEmpty(invoice.CustomerCountry))
                    column.Item().Text($"{invoice.CustomerCity} / {invoice.CustomerCountry}");

                // Entity: CustomerTaxNumber
                if (!string.IsNullOrEmpty(invoice.CustomerTaxNumber))
                    column.Item().Text($"VKN: {invoice.CustomerTaxNumber}");
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("#");
                    header.Cell().Element(CellStyle).Text("Hizmet / Ürün");
                    header.Cell().Element(CellStyle).AlignRight().Text("Birim Fiyat");
                    header.Cell().Element(CellStyle).AlignRight().Text("Adet");
                    header.Cell().Element(CellStyle).AlignRight().Text("Tutar");

                    static IContainer CellStyle(IContainer container) =>
                        container.DefaultTextStyle(x => x.SemiBold())
                                 .PaddingVertical(5)
                                 .BorderBottom(1)
                                 .BorderColor(Colors.Grey.Lighten2);
                });

                if (invoice.Lines != null)
                {
                    foreach (var item in invoice.Lines.Select((value, i) => new { i, value }))
                    {
                        table.Cell().Element(CellStyle).Text($"{item.i + 1}");
                        // Entity: Description
                        table.Cell().Element(CellStyle).Text(item.value.Description);
                        // Entity: UnitPrice
                        table.Cell().Element(CellStyle).AlignRight().Text($"{item.value.UnitPrice:N2} {invoice.Currency}");
                        // Entity: Quantity
                        table.Cell().Element(CellStyle).AlignRight().Text($"{item.value.Quantity}");
                        // Entity: TotalAmount (InvoiceLine içindeki)
                        table.Cell().Element(CellStyle).AlignRight().Text($"{item.value.TotalAmount:N2} {invoice.Currency}");

                        static IContainer CellStyle(IContainer container) =>
                            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten4).PaddingVertical(5);
                    }
                }
            });

            // Alt Toplamlar
            column.Item().PaddingTop(10).AlignRight().Column(col =>
            {
                // Entity: Subtotal
                col.Item().Text($"Ara Toplam: {invoice.Subtotal:N2} {invoice.Currency}");

                // Entity: TotalDiscount
                if (invoice.TotalDiscount > 0)
                    col.Item().Text($"İndirim: -{invoice.TotalDiscount:N2} {invoice.Currency}").FontColor(Colors.Red.Medium);

                // Entity: TotalTax
                if (invoice.TotalTax > 0)
                    col.Item().Text($"Vergi: {invoice.TotalTax:N2} {invoice.Currency}");

                // Entity: TotalAmount
                col.Item().PaddingTop(5)
                   .Text($"GENEL TOPLAM: {invoice.TotalAmount:N2} {invoice.Currency}")
                   .FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(x =>
            {
                x.Span("SubOrbit V2 Billing System - ");
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });

            row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("g"))
               .FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }

    #endregion
}