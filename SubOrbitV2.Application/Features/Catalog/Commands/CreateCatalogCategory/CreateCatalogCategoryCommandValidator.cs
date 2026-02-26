using FluentValidation;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateCatalogCategory;

public class CreateCatalogCategoryCommandValidator : AbstractValidator<CreateCatalogCategoryCommand>
{
    public CreateCatalogCategoryCommandValidator()
    {
        RuleFor(v => v.Code)
            .GreaterThan(0).WithMessage("Kategori kodu 0'dan büyük olmalıdır.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Kategori adı boş geçilemez.")
            .MaximumLength(100).WithMessage("Kategori adı 100 karakteri geçemez.");
    }
}