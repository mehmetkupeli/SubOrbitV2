using FluentValidation;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateFeature;

public class CreateFeatureCommandValidator : AbstractValidator<CreateFeatureCommand>
{
    public CreateFeatureCommandValidator()
    {
        RuleFor(v => v.FeatureGroupId)
            .NotEmpty().WithMessage("Bir özellik grubu seçilmelidir.");

        RuleFor(v => v.Key)
            .NotEmpty().WithMessage("Özellik anahtarı (Key) boş geçilemez.")
            .MaximumLength(50).WithMessage("Key 50 karakteri geçemez.")
            .Matches("^[a-zA-Z0-9_]*$").WithMessage("Key sadece harf, rakam ve alt tire (_) içerebilir.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Özellik adı boş geçilemez.")
            .MaximumLength(100).WithMessage("Özellik adı 100 karakteri geçemez.");

        RuleFor(v => v.DataType)
            .IsInEnum().WithMessage("Geçersiz veri tipi.");
    }
}