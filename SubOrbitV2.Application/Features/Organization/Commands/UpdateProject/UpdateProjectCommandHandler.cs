using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Organization;
using SubOrbitV2.Domain.Specifications.Organization;
using System.Text.Json;

namespace SubOrbitV2.Application.Features.Organization.Commands.UpdateProject;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly IFileService _fileService; // Logo işlemleri için eklendi
    private readonly IProjectContext _projectContext;

    public UpdateProjectCommandHandler(
        IUnitOfWork unitOfWork,
        IEncryptionService encryptionService,
        IFileService fileService,
        IProjectContext projectContext)
    {
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
        _fileService = fileService;
        _projectContext = projectContext;
    }

    public async Task<Result<bool>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        // 1. Projeyi Ayarlarıyla Beraber Getir
        var spec = new ProjectWithSettingsByIdSpecification(_projectContext.ProjectId);
        var project = await _unitOfWork.Repository<Project>().GetEntityWithSpec(spec);

        if (project == null)
            return Result<bool>.Failure("Proje bulunamadı.");

        #region 2. Temel Proje Güncellemeleri (Name, Desc, Logo)
        project.Name = request.Name;
        if (request.Description != null)
        {
            project.Description = request.Description;
        }

        // Yeni logo geldiyse, eskisini silip yenisini yükleyelim
        if (request.Logo != null)
        {
            if (!string.IsNullOrEmpty(project.LogoUrl))
            {
                await _fileService.DeleteFileAsync(project.LogoUrl);
            }
            project.LogoUrl = await _fileService.UploadFileAsync(request.Logo, "projects");
        }

        _unitOfWork.Repository<Project>().Update(project);
        #endregion

        #region 3. Ayarlar (Settings) Güncellemeleri
        if (project.Settings == null)
        {
            project.Settings = new ProjectSetting
            {
                ProjectId = project.Id,
                WebhookSecret = $"whsec_{Guid.NewGuid():N}"
            };
            await _unitOfWork.Repository<ProjectSetting>().AddAsync(project.Settings);
        }

        if (request.WebhookUrl != null)
            project.Settings.WebhookUrl = request.WebhookUrl;

        if (request.BillingConfig != null)
        {
            var billingJson = JsonSerializer.Serialize(request.BillingConfig);
            project.Settings.EncryptedBillingConfig = _encryptionService.Encrypt(billingJson);
        }

        if (request.SmtpConfig != null)
        {
            var smtpJson = JsonSerializer.Serialize(request.SmtpConfig);
            project.Settings.EncryptedSmtpConfig = _encryptionService.Encrypt(smtpJson);
        }

        _unitOfWork.Repository<ProjectSetting>().Update(project.Settings);
        #endregion

        // 4. Mühürle
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true, "Proje ve ayarları başarıyla güncellendi.");
    }
}