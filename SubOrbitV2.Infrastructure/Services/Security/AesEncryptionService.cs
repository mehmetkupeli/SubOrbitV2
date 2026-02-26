using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SubOrbitV2.Application.Common.Interfaces;

namespace SubOrbitV2.Infrastructure.Services.Security;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IOptions<SecuritySettings> settings)
    {
        // MasterKey string'ini byte dizisine çeviriyoruz. 
        // Key'in 32 byte (256 bit) olması beklenir, değilse MD5 ile 32 byte'a sabitliyoruz (Pratik çözüm).
        using var md5 = MD5.Create();
        _key = md5.ComputeHash(Encoding.UTF8.GetBytes(settings.Value.MasterKey));

        // Daha güvenli olması için SHA256 ile de 32 byte üretilebilir:
        // using var sha = SHA256.Create();
        // _key = sha.ComputeHash(Encoding.UTF8.GetBytes(settings.Value.MasterKey));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV(); // Rastgele IV üret

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();

        // IV'yi sonucun en başına yazıyoruz (16 byte)
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        // İlk 16 byte IV'dir, onu okuyoruz
        var iv = new byte[16];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}