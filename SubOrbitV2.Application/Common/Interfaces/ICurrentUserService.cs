namespace SubOrbitV2.Application.Common.Interfaces;

public interface ICurrentUserService
{
    /// <summary>
    /// O anki isteği yapan kullanıcının ID'si. Giriş yapmamışsa null döner.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Kullanıcının Tenant/Proje ID'si (Header'dan veya Token'dan gelir).
    /// </summary>
    Guid? ProjectId { get; }

    /// <summary>
    /// Kullanıcının Tenant Idsi (Header'dan veya Token'dan gelir).
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Kullanıcı giriş yapmış mı?
    /// </summary>
    bool IsAuthenticated { get; }
}