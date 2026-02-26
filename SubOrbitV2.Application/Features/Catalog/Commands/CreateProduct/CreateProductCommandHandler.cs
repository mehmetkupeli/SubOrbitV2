using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Specifications.Catalog;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<CreateProductResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectContext _projectContext;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork, IProjectContext projectContext)
    {
        _unitOfWork = unitOfWork;
        _projectContext = projectContext;
    }

    public async Task<Result<CreateProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        #region 1. SaaS İzolasyon Kontrolü
        var projectId = _projectContext.ProjectId;

        // Kategori gerçekten bu projeye mi ait? (Isolation Check)
        var category = await _unitOfWork.Repository<CatalogCategory>().GetByIdAsync(request.CatalogCategoryId);
        if (category == null || category.ProjectId != projectId)
            return Result<CreateProductResponse>.Failure("Seçilen kategori bulunamadı veya bu projeye ait değil.");
        #endregion

        #region 2. Code Tekillik Kontrolü
        var codeSpec = new ProductByCodeSpecification(projectId, request.Code);
        var existingProduct = await _unitOfWork.Repository<Product>().GetEntityWithSpec(codeSpec);

        if (existingProduct != null)
            return Result<CreateProductResponse>.Failure($"Bu projede '{request.Code}' kodlu bir ürün zaten mevcut.");
        #endregion

        #region 3. Oluşturma ve Kaydetme
        var product = new Product
        {
            CatalogCategoryId = request.CatalogCategoryId,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            IsHidden = request.IsHidden
        };

        await _unitOfWork.Repository<Product>().AddAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        return Result<CreateProductResponse>.Success(
            new CreateProductResponse(product.Id, product.Code, product.Name),
            "Ürün başarıyla oluşturuldu.");
    }
}