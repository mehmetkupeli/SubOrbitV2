namespace SubOrbitV2.Application.Common.Models;
public class ProjectDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string? EncryptedBillingConfig { get; set; } = null;
    public string? EncryptedSmtpConfig { get; set; } = null;
    public bool IsActive { get; set; }
}