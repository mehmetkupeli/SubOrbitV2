namespace SubOrbitV2.Application.Features.Organization.Queries.GetTenantProjects;

public record TenantProjectListItemDto(Guid Id, string Name, string Description, string? LogoUrl, string ApiKey, bool IsActive, DateTime CreatedAt);