namespace SubOrbitV2.Application.Features.Auth.Commands.Register;

public class RegisterTenantResponse
{
    public Guid TenantId { get; set; }
    public Guid AdminId { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}