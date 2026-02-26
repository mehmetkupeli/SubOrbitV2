using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Specifications.Catalog;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateCoupon;

public class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Result<CreateCouponResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectContext _projectContext;

    public CreateCouponCommandHandler(IUnitOfWork unitOfWork, IProjectContext projectContext)
    {
        _unitOfWork = unitOfWork;
        _projectContext = projectContext;
    }

    public async Task<Result<CreateCouponResponse>> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        var projectId = _projectContext.ProjectId;
        var normalizedCode = request.Code.ToUpperInvariant(); // Kuponları her zaman büyük harf kaydederiz

        #region 1. Güvenlik ve İzolasyon (Restricted Product)
        if (request.RestrictedProductId.HasValue)
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(request.RestrictedProductId.Value);
            if (product == null || product.ProjectId != projectId)
                return Result<CreateCouponResponse>.Failure("Kısıtlanmak istenen ürün bulunamadı veya bu projeye ait değil.");
        }
        #endregion

        #region 2. Tekillik Kontrolü
        var spec = new CouponByCodeSpecification(projectId, normalizedCode);
        var existingCoupon = await _unitOfWork.Repository<Coupon>().GetEntityWithSpec(spec);

        if (existingCoupon != null)
            return Result<CreateCouponResponse>.Failure($"Bu projede '{normalizedCode}' koduna sahip bir kupon zaten mevcut.");
        #endregion

        #region 3. Oluşturma
        var coupon = new Coupon
        {
            Code = normalizedCode,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            Duration = request.Duration,
            DurationInMonths = request.Duration == Domain.Enums.CouponDuration.Repeating ? request.DurationInMonths : null,
            ExpiryDate = request.ExpiryDate?.ToUniversalTime(), 
            MaxRedemptions = request.MaxRedemptions,
            IsActive = request.IsActive,
            IsPublic = request.IsPublic,
            RestrictedProductId = request.RestrictedProductId
        };

        await _unitOfWork.Repository<Coupon>().AddAsync(coupon);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        return Result<CreateCouponResponse>.Success(new CreateCouponResponse(coupon.Id, coupon.Code, coupon.DiscountValue),"Kupon başarıyla oluşturuldu.");
    }
}