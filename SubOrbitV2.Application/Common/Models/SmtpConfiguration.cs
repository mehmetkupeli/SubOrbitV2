namespace SubOrbitV2.Application.Common.Models;

/// <summary>
/// ProjectSetting.EncryptedSmtpConfig alanından deserialize edilecek yapı.
/// Her proje kendi mail sunucu ayarlarını burada tutar.
/// </summary>
public class SmtpConfiguration
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}