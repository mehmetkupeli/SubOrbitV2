namespace SubOrbitV2.Domain.Enums;

/// <summary>
/// Bir özelliğin (Feature) veri tipini belirler.
/// </summary>
public enum FeatureDataType
{
    /// <summary>
    /// Özellik sadece Var/Yok mantığıyla çalışır (True/False).
    /// Örn: "Fatura Oluşturabilir mi?", "API Erişimi Var mı?"
    /// </summary>
    Boolean = 1,

    /// <summary>
    /// Özellik sayısal bir limit veya değer belirtir.
    /// Örn: "Kullanıcı Limiti: 5", "Depolama Alanı (GB): 10".
    /// </summary>
    Integer = 2,

    /// <summary>
    /// Metinsel bir değer tutar (Nadir kullanılır ama esneklik sağlar).
    /// Örn: "Sunucu Lokasyonu: Frankfurt".
    /// </summary>
    Text = 3
}