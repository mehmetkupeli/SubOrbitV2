using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Specifications.Catalog;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreatePrice;

public class CreatePriceCommandHandler : IRequestHandler<CreatePriceCommand, Result<CreatePriceResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectContext _projectContext;

    public CreatePriceCommandHandler(IUnitOfWork unitOfWork, IProjectContext projectContext)
    {
        _unitOfWork = unitOfWork;
        _projectContext = projectContext;
    }

    public async Task<Result<CreatePriceResponse>> Handle(CreatePriceCommand request, CancellationToken cancellationToken)
    {
        var projectId = _projectContext.ProjectId;

        #region 1. SaaS İzolasyon Kontrolü
        // Fiyat eklenecek ürün gerçekten bu projeye mi ait?
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(request.ProductId);
        if (product == null || product.ProjectId != projectId)
            return Result<CreatePriceResponse>.Failure("Ürün bulunamadı veya bu projeye ait değil.");
        #endregion

        #region 2. Mükerrer Fiyat Kontrolü
        // Aynı ürüne, aynı periyotta ve aynı para biriminde ikinci bir fiyat açılamaz.
        var spec = new PriceUniquenessSpecification(
            projectId,
            request.ProductId,
            request.Currency,
            request.Interval,
            request.IntervalCount);

        var existingPrice = await _unitOfWork.Repository<Price>().GetEntityWithSpec(spec);

        if (existingPrice != null)
            return Result<CreatePriceResponse>.Failure($"Bu ürün için '{request.IntervalCount} {request.Interval}' periyodunda ve '{request.Currency}' para biriminde bir fiyat zaten tanımlanmış.");
        #endregion

        #region 3. Oluşturma
        var price = new Price
        {
            // ProjectId DbContext interceptor'dan geliyor.
            ProductId = request.ProductId,
            Name = request.Name,
            Amount = request.Amount,
            Currency = request.Currency.ToUpper(), // Standardizasyon
            VatRate = request.VatRate,
            Interval = request.Interval,
            IntervalCount = request.IntervalCount,
            TrialDays = request.TrialDays,
            IsActive = request.IsActive
        };

        await _unitOfWork.Repository<Price>().AddAsync(price);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        return Result<CreatePriceResponse>.Success(
            new CreatePriceResponse(price.Id, price.ProductId, price.Name, price.Amount, price.Currency),
            "Fiyat başarıyla oluşturuldu.");
    }
}