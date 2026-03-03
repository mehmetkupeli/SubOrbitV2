using FluentValidation;
using SubOrbitV2.Application.Features.Organization.Commands.CreateProject; // Alt validatorlar buradan geliyor

namespace SubOrbitV2.Application.Features.Organization.Commands.UpdateProject;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        // Temel Alanlar
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Proje adı boş geçilemez.")
            .MinimumLength(3).WithMessage("Proje adı en az 3 karakter olmalıdır.")
            .MaximumLength(100).WithMessage("Proje adı 100 karakteri geçemez.");

        RuleFor(v => v.Description)
            .MaximumLength(500).WithMessage("Açıklama 500 karakteri geçemez.");

        RuleFor(v => v.Logo)
            .Must(file => file == null || file.Length <= 2 * 1024 * 1024)
            .WithMessage("Logo boyutu 2MB'den büyük olamaz.")
            .Must(file => file == null || IsValidImage(file.ContentType))
            .WithMessage("Sadece .jpg, .jpeg, .png veya .webp formatında resim yükleyebilirsiniz.");

        // Ayar Alanları
        RuleFor(v => v.WebhookUrl)
            .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Geçerli bir Webhook URL formatı giriniz.");

    }

    private bool IsValidImage(string contentType)
    {
        var validTypes = new[] { "image/jpeg", "image/png", "image/jpg", "image/webp" };
        return validTypes.Contains(contentType);
    }
}