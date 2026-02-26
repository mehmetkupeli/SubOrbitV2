namespace SubOrbitV2.Application.Common.Interfaces;

/// <summary>
/// Hassas verileri (API Key, Connection String vb.) şifrelemek ve çözmek için kullanılır.
/// Genellikle AES-256 standardı kullanılır.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Düz metni şifreler.
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Şifreli metni çözer.
    /// </summary>
    string Decrypt(string cipherText);
}