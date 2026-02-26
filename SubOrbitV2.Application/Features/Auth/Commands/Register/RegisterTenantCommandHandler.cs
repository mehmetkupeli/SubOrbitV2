using MediatR;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Identity;
using SubOrbitV2.Domain.Entities.Organization;
using SubOrbitV2.Domain.Specifications.Identity;

namespace SubOrbitV2.Application.Features.Auth.Commands.Register;

public class RegisterTenantCommandHandler : IRequestHandler<RegisterTenantCommand, RegisterTenantResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IFileService _fileService;

    public RegisterTenantCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IFileService fileService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _fileService = fileService;
    }

    public async Task<RegisterTenantResponse> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        // 1. Email Kontrolü
        var userRepo = _unitOfWork.Repository<AppUser>();
        var spec = new UserByEmailSpecification(request.Email);
        var existingUser = await userRepo.GetEntityWithSpec(spec);

        if (existingUser != null)
        {
            return new RegisterTenantResponse
            {
                IsSuccess = false,
                Message = "Bu e-posta adresi zaten kullanımda."
            };
        }

        // 2. Logo İşleme
        string? logoUrl = null;
        if (request.Logo != null)
        {
            logoUrl = await _fileService.UploadFileAsync(request.Logo, "tenants");
        }

        // 3. Tenant Oluşturma (Tam Veri Setiyle)
        var tenant = new Tenant
        {
            Name = request.CompanyName,
            ContactEmail = request.Email,
            TaxNumber = request.TaxNumber, 
            Address = request.Address,     
            City = request.City,           
            Country = request.Country,     
            LogoUrl = logoUrl,
            IsActive = true
        };

        // 4. Admin User Oluşturma
        var adminUser = new AppUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsActive = true,
            Tenant = tenant 
        };

        // 5. Kayıt (UnitOfWork)
        await _unitOfWork.Repository<Tenant>().AddAsync(tenant);
        await userRepo.AddAsync(adminUser);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterTenantResponse
        {
            IsSuccess = true,
            TenantId = tenant.Id,
            AdminId = adminUser.Id,
            Message = "Kayıt işlemi başarılı."
        };
    }
}