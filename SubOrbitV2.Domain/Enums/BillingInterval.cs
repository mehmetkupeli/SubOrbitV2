namespace SubOrbitV2.Domain.Enums;

public enum BillingInterval
{
    /// <summary>
    /// Tek seferlik ödeme.
    /// </summary>
    OneTime = 0,

    /// <summary>
    /// Aylık döngü.
    /// </summary>
    Month = 1,

    /// <summary>
    /// Yıllık döngü.
    /// </summary>
    Year = 2
}