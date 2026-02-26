using MediatR;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Enums;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateCoupon;

public record CreateCouponCommand : IRequest<Result<CreateCouponResponse>>
{
    public string Code { get; init; } = string.Empty;
    public CouponDiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public CouponDuration Duration { get; init; }
    public int? DurationInMonths { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public int? MaxRedemptions { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsPublic { get; init; } = false;
    public Guid? RestrictedProductId { get; init; }
}