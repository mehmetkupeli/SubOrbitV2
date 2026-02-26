using MediatR;
using SubOrbitV2.Application.Common.Models;

namespace SubOrbitV2.Application.Features.Auth.Commands.Login;

public class LoginCommand : IRequest<Result<LoginResponse>>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}