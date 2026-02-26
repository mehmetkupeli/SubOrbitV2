namespace SubOrbitV2.Domain.Enums;

public enum BillingAlignmentStrategy
{
    /// <summary>
    /// Hizalama yok. Kayıt olduğu gün fatura kesim günüdür.
    /// </summary>
    None = 0,

    /// <summary>
    /// Sabit gün seçimi. (Örn: Her ayın 15'i).
    /// </summary>
    FixedDay = 1,

    /// <summary>
    /// Takvim Çeyrekleri (1 Ocak, 1 Nisan, 1 Temmuz, 1 Ekim).
    /// </summary>
    CalendarQuarter = 2,

    /// <summary>
    /// Yarı Yıl (1 Ocak, 1 Temmuz).
    /// </summary>
    CalendarHalfYear = 3,

    /// <summary>
    /// Takvim Yılı Başı (1 Ocak).
    /// </summary>
    CalendarYear = 4
}