using FluentValidation;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreatePrice;

public class CreatePriceCommandValidator : AbstractValidator<CreatePriceCommand>
{
    public CreatePriceCommandValidator()
    {
        RuleFor(v => v.ProductId)
            .NotEmpty().WithMessage("Bir ürün seçilmelidir.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Fiyat adı (faturada görünecek ad) boş geçilemez.")
            .MaximumLength(150).WithMessage("Fiyat adı 150 karakteri geçemez.");

        RuleFor(v => v.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Fiyat tutarı 0'dan küçük olamaz.");

        RuleFor(v => v.Currency)
            .NotEmpty().WithMessage("Para birimi boş geçilemez.")
            .Length(3).WithMessage("Para birimi 3 karakterli uluslararası formatta olmalıdır (Örn: USD, TRY, DKK).");

        RuleFor(v => v.VatRate)
            .InclusiveBetween(0, 100).WithMessage("KDV oranı %0 ile %100 arasında olmalıdır.");

        RuleFor(v => v.Interval)
            .IsInEnum().WithMessage("Geçersiz fatura periyodu.");

        RuleFor(v => v.IntervalCount)
            .GreaterThan(0).WithMessage("Döngü sayısı en az 1 olmalıdır.");

        RuleFor(v => v.TrialDays)
            .GreaterThanOrEqualTo(0).WithMessage("Deneme süresi negatif olamaz.");
    }
}