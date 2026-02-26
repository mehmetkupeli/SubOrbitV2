using FluentValidation;

namespace SubOrbitV2.Application.Features.Catalog.Commands.SetProductFeatureValue;

public class SetProductFeatureValueCommandValidator : AbstractValidator<SetProductFeatureValueCommand>
{
    public SetProductFeatureValueCommandValidator()
    {
        RuleFor(v => v.ProductId).NotEmpty().WithMessage("Ürün seçilmelidir.");
        RuleFor(v => v.FeatureId).NotEmpty().WithMessage("Özellik seçilmelidir.");
        RuleFor(v => v.Value).NotNull().WithMessage("Değer boş olamaz.");
    }
}