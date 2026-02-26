namespace SubOrbitV2.Infrastructure.Services.Security;

public class SecuritySettings
{
    public const string SectionName = "SecuritySettings";
    /// <summary>
    /// Şifreleme için kullanılan ana anahtar.
    /// En az 32 karakter (256 bit) olmalıdır!
    /// </summary>
    public string MasterKey { get; set; } = string.Empty;
}