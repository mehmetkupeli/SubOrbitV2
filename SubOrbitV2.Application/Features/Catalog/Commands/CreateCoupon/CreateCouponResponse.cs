namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateCoupon;

public record CreateCouponResponse(Guid Id, string Code, decimal DiscountValue);