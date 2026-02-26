using FluentValidation;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateFeatureGroup;

public class CreateFeatureGroupCommandValidator : AbstractValidator<CreateFeatureGroupCommand>
{
    public CreateFeatureGroupCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Grup adı boş geçilemez.")
            .MaximumLength(100).WithMessage("Grup adı 100 karakteri geçemez.");

        RuleFor(v => v.Description)
            .MaximumLength(500).WithMessage("Açıklama 500 karakteri geçemez.");
    }
}