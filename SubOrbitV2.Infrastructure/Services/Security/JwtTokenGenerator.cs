using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Identity;

namespace SubOrbitV2.Infrastructure.Services.Security;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    public string GenerateToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),            
            new(ClaimTypes.Role, "Admin"),
            new("TenantId", user.TenantId.ToString())
        };

        // Null check yaparak ekliyoruz
        if (!string.IsNullOrEmpty(user.FirstName))
            claims.Add(new(ClaimTypes.GivenName, user.FirstName));

        if (!string.IsNullOrEmpty(user.LastName))
            claims.Add(new(ClaimTypes.Surname, user.LastName));

        return CreateToken(claims);
    }

    public string GeneratePayerToken(Payer payer)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, payer.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, payer.Email),
            new("ProjectId", payer.ProjectId.ToString()), // Custom Claim
            new(ClaimTypes.Role, "Payer")
        };

        return CreateToken(claims);
    }

    private string CreateToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }


    #region Refresh Token Implementation

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false, // Yenileme sırasında aud/iss doğrulamasına takılmamak için
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            ValidateLifetime = false // KRİTİK: Süresi dolmuş token'ı okuyabilmek için false!
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Geçersiz token algoritması.");

        return principal;
    }
    #endregion
}