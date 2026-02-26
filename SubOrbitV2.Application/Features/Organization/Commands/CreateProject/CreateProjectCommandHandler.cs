using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Organization;
using System.Text.Json;

namespace SubOrbitV2.Application.Features.Organization.Commands.CreateProject;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Result<CreateProjectResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileService _fileService;
    private readonly IEncryptionService _encryptionService; // Güvenlik muhafızı

    public CreateProjectCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IFileService fileService,
        IEncryptionService encryptionService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _fileService = fileService;
        _encryptionService = encryptionService;
    }

    public async Task<Result<CreateProjectResponse>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        // 1. Tenant Kontrolü
        var tenantId = _currentUserService.TenantId;
        if (tenantId == null)
            return Result<CreateProjectResponse>.Failure("Bağlı olduğunuz firma (Tenant) bilgisi eksik.");

        // 2. Logo İşleme
        string? logoUrl = null;
        if (request.Logo != null)
        {
            logoUrl = await _fileService.UploadFileAsync(request.Logo, "projects");
        }

        // 3. Proje Nesnesi (ApiKey üretimi dahil)
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            TenantId = tenantId.Value,
            LogoUrl = logoUrl,
            ApiKey = $"so_live_{Guid.NewGuid():N}",
            IsActive = true
        };

        // 4. Kozmik Ayarların Hazırlanması (Şifreleme Süreci)
        var projectSetting = new ProjectSetting
        {
            WebhookUrl = request.WebhookUrl,
            WebhookSecret = $"whsec_{Guid.NewGuid():N}",
            Project = project
        };

        // Ödeme Ayarlarını Şifrele
        if (request.BillingConfig != null)
        {
            var billingJson = JsonSerializer.Serialize(request.BillingConfig);
            projectSetting.EncryptedBillingConfig = _encryptionService.Encrypt(billingJson);
        }

        // SMTP Ayarlarını Şifrele
        if (request.SmtpConfig != null)
        {
            var smtpJson = JsonSerializer.Serialize(request.SmtpConfig);
            projectSetting.EncryptedSmtpConfig = _encryptionService.Encrypt(smtpJson);
        }

        // 5. Veritabanına Yazım (Atomik İşlem)
        await _unitOfWork.Repository<Project>().AddAsync(project);
        await _unitOfWork.Repository<ProjectSetting>().AddAsync(projectSetting);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CreateProjectResponse>.Success(
            new CreateProjectResponse(project.Id, project.ApiKey, project.Name),
            "Proje ve güvenlik ayarları başarıyla oluşturuldu.");
    }
}