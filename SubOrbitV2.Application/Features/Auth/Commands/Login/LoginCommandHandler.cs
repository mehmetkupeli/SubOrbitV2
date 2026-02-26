using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Identity;
using SubOrbitV2.Domain.Specifications.Identity;

namespace SubOrbitV2.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IUnitOfWork _unitOfWork; // Güncelleme işlemi için UnitOfWork şart
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        #region 1. Kullanıcıyı Getir ve Doğrula
        var spec = new UserByEmailSpecification(request.Email);
        var user = await _unitOfWork.Repository<AppUser>().GetEntityWithSpec(spec);

        if (user == null)
        {
            return Result<LoginResponse>.Failure("Kullanıcı bulunamadı.");
        }

        if (!user.IsActive)
        {
            return Result<LoginResponse>.Failure("Hesap pasif durumda.");
        }

        // Şifre kontrolü (Interface'deki isme göre Verify kullanıldı)
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<LoginResponse>.Failure("Hatalı şifre.");
        }
        #endregion

        #region 2. Token Üretimi ve Kaydı
        // Access Token
        var token = _tokenGenerator.GenerateToken(user);

        // Refresh Token
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        // Veritabanına Refresh Token bilgisini işle
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Örn: 7 günlük ömür

        _unitOfWork.Repository<AppUser>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        #region 3. Yanıtı Hazırla
        var fullName = $"{user.FirstName} {user.LastName}";

        var response = new LoginResponse(
            user.Id,
            fullName,
            user.Email,
            token,
            refreshToken);

        return Result<LoginResponse>.Success(response, "Giriş başarılı.");
        #endregion
    }
}