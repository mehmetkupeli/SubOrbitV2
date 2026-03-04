using FluentValidation;

namespace SubOrbitV2.Application.Features.Billing.Commands.AddSubscription;

public class AddSubscriptionCommandValidator : AbstractValidator<AddSubscriptionCommand>
{
    public AddSubscriptionCommandValidator()
    {
        RuleFor(x => x.PayerExternalId).NotEmpty().WithMessage("Ödemeyi yapacak ana müşteri (PayerExternalId) belirtilmelidir.");
        RuleFor(x => x.SubscriptionExternalId).NotEmpty().WithMessage("Paketin tahsis edileceği alt kullanıcı (SubscriptionExternalId) belirtilmelidir.");
        RuleFor(x => x.PriceId).NotEmpty().WithMessage("Bir fiyat paketi (PriceId) seçilmelidir.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Adet en az 1 olmalıdır.");
    }
}