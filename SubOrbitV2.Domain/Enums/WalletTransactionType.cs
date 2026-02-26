namespace SubOrbitV2.Domain.Enums;

public enum WalletTransactionType
{
    /// <summary>
    /// Sisteme para girişi (Örn: Manuel yükleme veya İade).
    /// </summary>
    Deposit = 1,

    /// <summary>
    /// Sistemden para çıkışı (Örn: Fatura ödemesi).
    /// </summary>
    Payment = 2,

    /// <summary>
    /// Abonelik iptali veya düşürme (Downgrade) kaynaklı iade.
    /// </summary>
    Refund = 3,

    /// <summary>
    /// Item bakiyesinden Payer bakiyesine aktarım.
    /// </summary>
    Transfer = 4,

    /// <summary>
    /// Sistem yöneticisi tarafından yapılan düzeltme.
    /// </summary>
    Adjustment = 5
}