using FluentValidation;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(v => v.CatalogCategoryId)
            .NotEmpty().WithMessage("Ürün bir kategoriye bağlı olmalıdır.");

        RuleFor(v => v.Code)
            .GreaterThan(0).WithMessage("Ürün kodu 0'dan büyük olmalıdır.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Ürün adı boş geçilemez.")
            .MaximumLength(150).WithMessage("Ürün adı 150 karakteri geçemez.");

        RuleFor(v => v.Description)
            .MaximumLength(1000).WithMessage("Açıklama 1000 karakteri geçemez.");
    }
}