using SubOrbitV2.Domain.Entities.Billing;

namespace SubOrbitV2.Application.Common.Interfaces;

/// <summary>
/// Sistemdeki belgeleri (Fatura vb.) PDF formatına çeviren servis.
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Verilen Fatura entity'sini şık bir PDF dosyasına dönüştürür.
    /// Not: Faturanın Payer ve InvoiceLines ilişkilerinin dolu (Include edilmiş) olması gerekir.
    /// </summary>
    /// <param name="invoice">Dolu fatura nesnesi.</param>
    /// <returns>PDF dosyasının byte dizisi.</returns>
    Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice);
}