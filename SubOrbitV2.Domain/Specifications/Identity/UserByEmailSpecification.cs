using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Identity;

namespace SubOrbitV2.Domain.Specifications.Identity;

public class UserByEmailSpecification : BaseSpecification<AppUser>
{
    public UserByEmailSpecification(string email): base(u => u.Email == email)
    {
    }
}