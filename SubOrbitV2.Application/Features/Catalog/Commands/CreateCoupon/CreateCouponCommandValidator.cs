using FluentValidation;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateCoupon;

public class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponCommandValidator()
    {
        RuleFor(v => v.Code)
            .NotEmpty().WithMessage("Kupon kodu boş geçilemez.")
            .MaximumLength(50).WithMessage("Kupon kodu 50 karakteri geçemez.")
            .Matches("^[a-zA-Z0-9_-]*$").WithMessage("Kupon kodu sadece harf, rakam, tire (-) ve alt tire (_) içerebilir.");

        RuleFor(v => v.DiscountType).IsInEnum().WithMessage("Geçersiz indirim türü.");
        RuleFor(v => v.Duration).IsInEnum().WithMessage("Geçersiz kupon süresi türü.");

        RuleFor(v => v.DiscountValue)
            .GreaterThan(0).WithMessage("İndirim değeri 0'dan büyük olmalıdır.");

        // Eğer indirim Yüzdelik ise, 100'den büyük olamaz.
        RuleFor(v => v.DiscountValue)
            .LessThanOrEqualTo(100).When(v => v.DiscountType == CouponDiscountType.Percentage)
            .WithMessage("Yüzdelik indirimler %100'den büyük olamaz.");

        // Eğer kupon 'Repeating' (Tekrarlayan) ise, kaç ay tekrarlayacağı girilmek zorundadır.
        RuleFor(v => v.DurationInMonths)
            .NotNull().GreaterThan(0)
            .When(v => v.Duration == CouponDuration.Repeating)
            .WithMessage("Tekrarlayan (Repeating) kuponlar için 'DurationInMonths' (geçerlilik ay sayısı) girilmelidir.");

        RuleFor(v => v.MaxRedemptions)
            .GreaterThan(0).When(v => v.MaxRedemptions.HasValue)
            .WithMessage("Maksimum kullanım limiti 0'dan büyük olmalıdır.");
    }
}