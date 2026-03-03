using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubOrbitV2.Application.Common.Attributes;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models;
using SubOrbitV2.Application.Features.Organization.Commands.CreateProject;
using SubOrbitV2.Application.Features.Organization.Commands.UpdateProject;
using SubOrbitV2.Application.Features.Organization.Queries.GetTenantProjects;

namespace SubOrbitV2.Api.Controllers.Organization;

[Route("api/projects")]
[ApiController]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ISender _sender;

    public ProjectsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("create")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateProjectCommand command)
    {
        var result = await _sender.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Projenin temel bilgilerini (İsim, Logo vb.) ve hassas ayarlarını (SMTP, Nexi) günceller.
    /// Resim yüklemesi içerdiği için form-data olarak gönderilmelidir.
    /// </summary>
    [HttpPut("update")]
    [Consumes("multipart/form-data")]
    [MustHaveProject]
    public async Task<IActionResult> UpdateProject([FromForm] UpdateProjectCommand command)
    {
        var result = await _sender.Send(command);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<TenantProjectListItemDto>>> GetTenantProjects([FromQuery] GetTenantProjectsQuery query)
    {
        return Ok(await _sender.Send(query));
    }
}