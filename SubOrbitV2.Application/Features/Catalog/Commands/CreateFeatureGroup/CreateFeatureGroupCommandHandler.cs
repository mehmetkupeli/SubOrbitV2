using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Specifications.Catalog;

namespace SubOrbitV2.Application.Features.Catalog.Commands.CreateFeatureGroup;

public class CreateFeatureGroupCommandHandler : IRequestHandler<CreateFeatureGroupCommand, Result<CreateFeatureGroupResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectContext _projectContext;

    public CreateFeatureGroupCommandHandler(IUnitOfWork unitOfWork, IProjectContext projectContext)
    {
        _unitOfWork = unitOfWork;
        _projectContext = projectContext;
    }

    public async Task<Result<CreateFeatureGroupResponse>> Handle(CreateFeatureGroupCommand request, CancellationToken cancellationToken)
    {
        #region 1. Bağlam Kontrolü
        var projectId = _projectContext.ProjectId;
        if (projectId == Guid.Empty)
            return Result<CreateFeatureGroupResponse>.Failure("İşlem yapılacak proje belirlenemedi.");
        #endregion

        #region 2. Tekillik Kontrolü (Global Casing)
        // Invariant-friendly tekillik kontrolü
        var spec = new FeatureGroupByNameSpecification(projectId, request.Name);
        var exists = await _unitOfWork.Repository<FeatureGroup>().GetEntityWithSpec(spec);

        if (exists!=null)
            return Result<CreateFeatureGroupResponse>.Failure($"'{request.Name}' ismiyle bir özellik grubu zaten mevcut.");
        #endregion

        #region 3. Kayıt (Auto ProjectId)
        var featureGroup = new FeatureGroup
        {
            Name = request.Name,
            Description = request.Description
        };

        await _unitOfWork.Repository<FeatureGroup>().AddAsync(featureGroup);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        return Result<CreateFeatureGroupResponse>.Success(
            new CreateFeatureGroupResponse(featureGroup.Id, featureGroup.Name),
            "Feature Group oluşturuldu.");
    }
}