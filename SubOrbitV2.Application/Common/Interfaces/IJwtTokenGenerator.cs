using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Identity;
using System.Security.Claims;

namespace SubOrbitV2.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    /// <summary>
    /// Tenant Yöneticisi (Admin) için Access Token üretir.
    /// İçinde UserId, Email ve Roller bulunur.
    /// </summary>
    string GenerateToken(AppUser user);

    /// <summary>
    /// Müşteri Portalı (Payer) için kısıtlı Access Token üretir.
    /// İçinde PayerId, ProjectId ve 'Payer' rolü bulunur.
    /// </summary>
    string GeneratePayerToken(Payer payer);

    #region Refresh Token Methods
    /// <summary>
    /// Güvenli, kriptografik rastgele bir Refresh Token üretir.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Süresi dolmuş bir token'dan kullanıcı bilgilerini (ClaimsPrincipal) güvenli bir şekilde çıkarır.
    /// </summary>
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    #endregion
}