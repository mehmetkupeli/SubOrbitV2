using MediatR;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Billing.Commands.AddSubscription;

public record AddSubscriptionCommand : IRequest<Result<bool>>
{
    /// <summary>
    /// Ödemeyi yapacak olan ANA cüzdanın (Payer) ID'si. (Örn: Ahmet'in ID'si)
    /// </summary>
    public string PayerExternalId { get; init; } = string.Empty;

    /// <summary>
    /// Bu yeni paketin tahsis edileceği ALT kullanıcının ID'si. (Örn: Yeni eklenen Admin'in ID'si)
    /// </summary>
    public string SubscriptionExternalId { get; init; } = string.Empty;

    /// <summary>
    /// Satın alınacak eklenti/koltuk paketi (Price).
    /// </summary>
    public Guid PriceId { get; init; }

    public string? CouponCode { get; init; }

    public int Quantity { get; init; } = 1;

    public string? Label { get; init; }
}