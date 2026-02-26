using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Catalog;
using SubOrbitV2.Domain.Enums;
using SubOrbitV2.Domain.Specifications.Catalog;

namespace SubOrbitV2.Application.Features.Catalog.Commands.SetProductFeatureValue;

public class SetProductFeatureValueCommandHandler : IRequestHandler<SetProductFeatureValueCommand, Result<SetProductFeatureValueResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectContext _projectContext;

    public SetProductFeatureValueCommandHandler(IUnitOfWork unitOfWork, IProjectContext projectContext)
    {
        _unitOfWork = unitOfWork;
        _projectContext = projectContext;
    }

    public async Task<Result<SetProductFeatureValueResponse>> Handle(SetProductFeatureValueCommand request, CancellationToken cancellationToken)
    {
        var projectId = _projectContext.ProjectId;

        #region 1. SaaS İzolasyon Kontrolleri (Güvenlik)
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(request.ProductId);
        if (product == null || product.ProjectId != projectId)
            return Result<SetProductFeatureValueResponse>.Failure("Ürün bulunamadı veya bu projeye ait değil.");

        var feature = await _unitOfWork.Repository<Feature>().GetByIdAsync(request.FeatureId);
        if (feature == null || feature.ProjectId != projectId)
            return Result<SetProductFeatureValueResponse>.Failure("Özellik bulunamadı veya bu projeye ait değil.");
        #endregion

        #region 2. DataType Validasyonu (Akıllı Kontrol)
        var trimmedValue = request.Value.Trim();
        switch (feature.DataType)
        {
            case FeatureDataType.Boolean:
                if (!bool.TryParse(trimmedValue, out _))
                    return Result<SetProductFeatureValueResponse>.Failure($"Bu özellik Boolean tipindedir. Lütfen 'true' veya 'false' giriniz.");
                trimmedValue = trimmedValue.ToLower(); // Standartlaştırma
                break;

            case FeatureDataType.Integer:
                if (!int.TryParse(trimmedValue, out _))
                    return Result<SetProductFeatureValueResponse>.Failure($"Bu özellik Integer tipindedir. Lütfen tam sayı giriniz. (Sınırsız için -1)");
                break;

            case FeatureDataType.Text:
                if (trimmedValue.Length > 255) // Mantıksal bir limit
                    return Result<SetProductFeatureValueResponse>.Failure("Metinsel değerler 255 karakteri geçemez.");
                break;
        }
        #endregion

        #region 3. Upsert İşlemi (Varsa Güncelle, Yoksa Ekle)
        var spec = new ProductFeatureValueSpecification(projectId, request.ProductId, request.FeatureId);
        var existingValue = await _unitOfWork.Repository<ProductFeatureValue>().GetEntityWithSpec(spec);

        Guid resultId;

        if (existingValue != null)
        {
            // Güncelleme
            existingValue.Value = trimmedValue;
            _unitOfWork.Repository<ProductFeatureValue>().Update(existingValue);
            resultId = existingValue.Id;
        }
        else
        {
            // Yeni Ekleme
            var newValue = new ProductFeatureValue
            {
                ProductId = request.ProductId,
                FeatureId = request.FeatureId,
                Value = trimmedValue
                // ProjectId otomatik setleniyor
            };
            await _unitOfWork.Repository<ProductFeatureValue>().AddAsync(newValue);
            resultId = newValue.Id;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        return Result<SetProductFeatureValueResponse>.Success(
            new SetProductFeatureValueResponse(resultId, request.ProductId, request.FeatureId, trimmedValue),
            "Özellik değeri başarıyla kaydedildi.");
    }
}