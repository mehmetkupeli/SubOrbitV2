using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Identity;
using System.Security.Claims;

namespace SubOrbitV2.Application.Features.Auth.Commands.RotateToken;

public class RotateTokenCommandHandler : IRequestHandler<RotateTokenCommand, Result<RotateTokenResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RotateTokenCommandHandler(IUnitOfWork unitOfWork, IJwtTokenGenerator jwtTokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<RotateTokenResponse>> Handle(RotateTokenCommand request, CancellationToken cancellationToken)
    {
        #region 1. Token Analizi
        var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(request.AccessToken);
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Result<RotateTokenResponse>.Failure("Token içeriği geçersiz.");
        #endregion

        #region 2. Kullanıcı ve Refresh Token Doğrulama
        var user = await _unitOfWork.Repository<AppUser>().GetByIdAsync(userId);

        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return Result<RotateTokenResponse>.Failure("Oturum süresi dolmuş veya geçersiz refresh token.");
        #endregion

        #region 3. Token Rotation (Döndürme)
        // Yeni ikiliyi üret
        var newAccessToken = _jwtTokenGenerator.GenerateToken(user);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        // Veritabanını güncelle
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Örn: 7 gün

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        return Result<RotateTokenResponse>.Success(new RotateTokenResponse(newAccessToken, newRefreshToken), "Oturum başarıyla yenilendi.");
    }
}