using FluentValidation;

namespace SubOrbitV2.Application.Features.Auth.Commands.Register;

public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        // Firma Validasyonları
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Firma adı gereklidir.")
            .MaximumLength(100).WithMessage("Firma adı 100 karakteri geçemez.");

        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Vergi numarası gereklidir.")
            .MaximumLength(50).WithMessage("Vergi numarası çok uzun.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Adres bilgisi gereklidir.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Şehir bilgisi gereklidir.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Ülke bilgisi gereklidir.");

        // Admin Validasyonları
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad gereklidir.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad gereklidir.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta gereklidir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre gereklidir.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");

        // Logo Validasyonu
        RuleFor(x => x.Logo)
            .Must(file => file == null || file.ContentType.StartsWith("image/"))
            .WithMessage("Sadece resim dosyaları (jpg, png vb.) yüklenebilir.");
    }
}