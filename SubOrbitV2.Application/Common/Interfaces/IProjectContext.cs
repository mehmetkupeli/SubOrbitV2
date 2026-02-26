using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Domain.Entities.Organization;

namespace SubOrbitV2.Application.Common.Interfaces;

/// <summary>
/// O anki HTTP isteğinin hangi projeye (Tenant/Project) ait olduğunu tutar.
/// Middleware tarafından doldurulur, Repository'ler tarafından okunur.
/// </summary>
public interface IProjectContext
{
    /// <summary>
    /// Aktif Proje ID'si.
    /// </summary>
    Guid ProjectId { get; }

    /// <summary>
    /// Proje ID'sinin set edilip edilmediğini kontrol eder.
    /// </summary>
    bool IsIdSet { get; }

    /// <summary>
    /// Middleware'in ID'yi set etmesi için kullanılır.
    /// </summary>
    void SetProjectId(Guid projectId);

    ProjectDto? CurrentProject { get; }
    void SetProject(Project project);
}