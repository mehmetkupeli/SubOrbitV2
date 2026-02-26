namespace SubOrbitV2.Infrastructure.Services.Payment.Nexi;

/// <summary>
/// appsettings.json dosyasındaki "Nexi" bölümünü temsil eder.
/// Tüm tenantlar için ortak olan API uç noktalarını barındırır.
/// </summary>
public class NexiSettings
{
    public const string SectionName = "NexiSettings";
    public string BaseUrl { get; set; } = string.Empty;
    public string PublicApiUrl { get; set; } = string.Empty;
}