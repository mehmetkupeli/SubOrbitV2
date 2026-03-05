namespace SubOrbitV2.Application.Common.Utils;

public static class InvoiceHelper
{
    /// <summary>
    /// Standart fatura numarası üretir (Örn: INV-20260305-A1B2C3)
    /// </summary>
    public static string GenerateInvoiceNumber()
    {
        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
    }
    
}