using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Specifications.Catalog;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateFeature;

public class CreateFeatureCommandHandler : IRequestHandler<CreateFeatureCommand, Result<CreateFeatureResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectContext _projectContext;

    public CreateFeatureCommandHandler(IUnitOfWork unitOfWork, IProjectContext projectContext)
    {
        _unitOfWork = unitOfWork;
        _projectContext = projectContext;
    }

    public async Task<Result<CreateFeatureResponse>> Handle(CreateFeatureCommand request, CancellationToken cancellationToken)
    {
        #region 1. Bağlam ve Grup Kontrolü
        var projectId = _projectContext.ProjectId;

        // Önce grubun varlığını ve bu projeye ait olduğunu kontrol edelim (Senior Check)
        var group = await _unitOfWork.Repository<FeatureGroup>().GetByIdAsync(request.FeatureGroupId);
        if (group == null || group.ProjectId != projectId)
            return Result<CreateFeatureResponse>.Failure("Seçilen özellik grubu bulunamadı veya bu projeye ait değil.");
        #endregion

        #region 2. Key Tekillik Kontrolü
        var spec = new FeatureByKeySpecification(projectId, request.Key);
        var existingFeature = await _unitOfWork.Repository<Feature>().GetEntityWithSpec(spec);

        if (existingFeature != null)
            return Result<CreateFeatureResponse>.Failure($"'{request.Key}' anahtarı ile tanımlanmış bir özellik zaten mevcut.");
        #endregion

        #region 3. Oluşturma
        var feature = new Feature
        {
            // ProjectId DbContext'te otomatik setlenecek
            FeatureGroupId = request.FeatureGroupId,
            Key = request.Key,
            Name = request.Name,
            DataType = request.DataType
        };

        await _unitOfWork.Repository<Feature>().AddAsync(feature);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        return Result<CreateFeatureResponse>.Success(
            new CreateFeatureResponse(feature.Id, feature.Key, feature.Name),
            "Feature başarıyla tanımlandı.");
    }
}