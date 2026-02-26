namespace SubOrbitV2.Application.Common.Interfaces;

/// <summary>
/// Kullanıcı şifrelerini güvenli bir şekilde hash'lemek ve doğrulamak için kullanılır.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Şifreyi hash'ler (Saltlama dahil).
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Girilen şifrenin, veritabanındaki hash ile eşleşip eşleşmediğini kontrol eder.
    /// </summary>
    bool Verify(string password, string hashedPassword);
}