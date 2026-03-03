using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Organization;

namespace SubOrbitV2.Domain.Specifications.Organization;

public class ProjectWithSettingsByIdSpecification : BaseSpecification<Project>
{
    public ProjectWithSettingsByIdSpecification(Guid projectId)
        : base(x => x.Id == projectId)
    {
        AddInclude(x => x.Settings);
    }
}