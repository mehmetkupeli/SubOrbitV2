using MediatR;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Application.Features.Billing.Commands.InitiateSubscription;

public record InitiateSubscriptionCommand : IRequest<Result<InitiateSubscriptionResponse>>
{
    // Hizmet verdiğimiz firmanın sistemindeki Kullanıcı/Firma ID'si
    public string ExternalId { get; init; } = string.Empty;

    // Satın alınmak istenen paket (Price)
    public Guid PriceId { get; init; }
    public string? CouponCode { get; init; }

    // Nexi'den dönülecek sayfa
    public string ReturnUrl { get; init; } = string.Empty;

    // Fatura (Payer) Bilgileri
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? TaxOffice { get; init; }
    public string? TaxNumber { get; init; }
    public string? BillingAddress { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }

    // Hizalama (Alignment) Ayarları
    public BillingAlignmentStrategy AlignmentStrategy { get; init; } = BillingAlignmentStrategy.None;
    public int BillingAnchorDay { get; init; } = 1;
}