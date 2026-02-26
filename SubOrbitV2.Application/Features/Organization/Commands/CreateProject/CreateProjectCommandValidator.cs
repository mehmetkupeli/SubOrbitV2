using FluentValidation;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Organization.Commands.CreateProject;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        // 1. Temel Proje Bilgileri
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Proje adı boş geçilemez.")
            .MinimumLength(3).WithMessage("Proje adı en az 3 karakter olmalıdır.")
            .MaximumLength(100).WithMessage("Proje adı 100 karakteri geçemez.");

        RuleFor(v => v.Description)
            .MaximumLength(500).WithMessage("Açıklama 500 karakteri geçemez.");

        // 2. Webhook URL Validasyonu (Eğer girilmişse)
        RuleFor(v => v.WebhookUrl)
            .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Geçerli bir Webhook URL formatı giriniz.");

        // 3. Logo Validasyonu (Eğer yüklenmişse)
        RuleFor(v => v.Logo)
            .Must(file => file == null || file.Length <= 2 * 1024 * 1024)
            .WithMessage("Logo boyutu 2MB'den büyük olamaz.")
            .Must(file => file == null || IsValidImage(file.ContentType))
            .WithMessage("Sadece .jpg, .jpeg, .png veya .webp formatında resim yükleyebilirsiniz.");

        // 4. Billing Config Validasyonu (Eğer obje gönderilmişse)
        RuleSet("Billing", () => {
            RuleFor(v => v.BillingConfig)
                .SetValidator(new BillingConfigValidator()!)
                .When(v => v.BillingConfig != null);
        });

        // 5. SMTP Config Validasyonu (Eğer obje gönderilmişse)
        RuleSet("Smtp", () => {
            RuleFor(v => v.SmtpConfig)
                .SetValidator(new SmtpConfigurationValidator()!)
                .When(v => v.SmtpConfig != null);
        });
    }

    private bool IsValidImage(string contentType)
    {
        var validTypes = new[] { "image/jpeg", "image/png", "image/jpg", "image/webp" };
        return validTypes.Contains(contentType);
    }
}

// --- ALT VALIDATORLAR ---

public class BillingConfigValidator : AbstractValidator<BillingConfig>
{
    public BillingConfigValidator()
    {
        RuleFor(x => x.SecretKey)
            .NotEmpty().WithMessage("Ödeme sistemi Secret Key boş olamaz.");

        RuleFor(x => x.CheckoutKey)
            .NotEmpty().WithMessage("Ödeme sayfası Checkout Key boş olamaz.");
    }
}

public class SmtpConfigurationValidator : AbstractValidator<SmtpConfiguration>
{
    public SmtpConfigurationValidator()
    {
        RuleFor(x => x.Host).NotEmpty().WithMessage("SMTP Host boş olamaz.");
        RuleFor(x => x.Port).InclusiveBetween(1, 65535).WithMessage("Geçersiz Port numarası.");
        RuleFor(x => x.UserName).NotEmpty().WithMessage("SMTP Kullanıcı adı boş olamaz.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("SMTP Şifresi boş olamaz.");
        RuleFor(x => x.FromEmail).EmailAddress().WithMessage("Geçerli bir gönderici e-posta adresi giriniz.");
    }
}