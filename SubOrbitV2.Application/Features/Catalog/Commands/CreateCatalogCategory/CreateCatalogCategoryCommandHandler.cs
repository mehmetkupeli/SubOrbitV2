using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Specifications.Catalog;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateCatalogCategory;

public class CreateCatalogCategoryCommandHandler : IRequestHandler<CreateCatalogCategoryCommand, Result<CreateCatalogCategoryResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectContext _projectContext;

    public CreateCatalogCategoryCommandHandler(IUnitOfWork unitOfWork, IProjectContext projectContext)
    {
        _unitOfWork = unitOfWork;
        _projectContext = projectContext;
    }

    public async Task<Result<CreateCatalogCategoryResponse>> Handle(CreateCatalogCategoryCommand request, CancellationToken cancellationToken)
    {
        #region 1. Proje Bağlamı Kontrolü
        var projectId = _projectContext.ProjectId;
        if (projectId == Guid.Empty)
            return Result<CreateCatalogCategoryResponse>.Failure("İşlem yapılacak proje kimliği bulunamadı.");
        #endregion

        #region 2. İş Kuralları ve Validasyon (SENIOR CHECK)
        var duplicateSpec = new CatalogCategoryByCodeSpecification(projectId, request.Code);
        var existingCategoryCount = await _unitOfWork.Repository<CatalogCategory>().CountAsync(duplicateSpec);

        if (existingCategoryCount > 0)
        {
            return Result<CreateCatalogCategoryResponse>.Failure($"Bu projede '{request.Code}' kodu ile zaten bir kategori mevcut. Lütfen farklı bir kod deneyiniz.");
        }
        #endregion

        #region 3. Kategori Oluşturma
        var category = new CatalogCategory
        {
            Code = request.Code,
            Name = request.Name,
            IsActive = true
        };

        await _unitOfWork.Repository<CatalogCategory>().AddAsync(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        #region 4. Yanıt
        var response = new CreateCatalogCategoryResponse(category.Id, category.Code, category.Name);
        return Result<CreateCatalogCategoryResponse>.Success(response, "Katalog kategorisi başarıyla oluşturuldu.");
        #endregion
    }
}