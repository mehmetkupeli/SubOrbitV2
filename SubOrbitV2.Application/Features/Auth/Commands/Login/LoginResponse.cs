namespace SubOrbitV2.Application.Features.Auth.Commands.Login;

/// <summary>
/// Giriş işlemi sonrası dönülen veriler.
/// </summary>
public record LoginResponse(
    Guid UserId,
    string FullName,
    string Email,
    string Token,
    string RefreshToken); 