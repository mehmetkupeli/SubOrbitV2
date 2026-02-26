using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Entities.Organization;

namespace SubOrbitV2.Infrastructure.Services.Identity;

public class ProjectContext : IProjectContext
{
    private ProjectDto? _currentProject;
    private Guid _projectId;
    private bool _isIdSet;

    public Guid ProjectId
    {
        get
        {
            if (!_isIdSet)
                return Guid.Empty;

            return _projectId;
        }
    }

    public bool IsIdSet => _isIdSet;

    public ProjectDto? CurrentProject => _currentProject;

    public void SetProject(Project project)
    {
        _currentProject = MapToDto(project);
    }

    public void SetProjectId(Guid projectId)
    {
        // Bir istek sırasında ID sadece bir kez set edilmelidir (Güvenlik için).
        if (_isIdSet) return;

        _projectId = projectId;
        _isIdSet = true;
    }

    private static ProjectDto MapToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            TenantId = project.TenantId,
            Name = project.Name,
            Description = project.Description,
            ApiKey = project.ApiKey,
            WebhookUrl = project.Settings.WebhookUrl,
            IsActive = project.IsActive,
            EncryptedSmtpConfig = project.Settings.EncryptedSmtpConfig,
            EncryptedBillingConfig = project.Settings.EncryptedBillingConfig
        };
    }
}