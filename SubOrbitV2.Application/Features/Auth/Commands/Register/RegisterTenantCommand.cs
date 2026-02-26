using MediatR;
using Microsoft.AspNetCore.Http;

namespace SubOrbitV2.Application.Features.Auth.Commands.Register;

public class RegisterTenantCommand : IRequest<RegisterTenantResponse>
{
    // Firma Bilgileri
    public string CompanyName { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    // Admin Bilgileri
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Medya
    public IFormFile? Logo { get; set; }
}