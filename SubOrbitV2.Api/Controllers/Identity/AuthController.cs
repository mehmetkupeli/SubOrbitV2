using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubOrbitV2.Application.Features.Auth.Commands.Login;
using SubOrbitV2.Application.Features.Auth.Commands.Register;
using SubOrbitV2.Application.Features.Auth.Commands.RotateToken;

namespace SubOrbitV2.Api.Controllers.Identity;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Yeni Tenant (Firma) ve Admin kaydı oluşturur.
    /// Logo yüklenebileceği için "multipart/form-data" formatında veri bekler.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromForm] RegisterTenantCommand command)
    {
        var result = await _sender.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Sisteme giriş yapar ve JWT Token döner.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _sender.Send(command);

        if (!result.IsSuccess)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpPost("rotate-token")]
    public async Task<IActionResult> RotateToken([FromBody] RotateTokenCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
    }
}