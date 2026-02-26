using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubOrbitV2.Application.Common.Attributes;
using SubOrbitV2.Application.Features.Catalog.Commands.CreateFeatureGroup;

namespace SubOrbitV2.Api.Controllers.Catalog;

[Route("api/catalog/feature-groups")]
[ApiController]
[Authorize]
[MustHaveProject] // X-Project-Id header'ı zorunlu
public class FeatureGroupsController : ControllerBase
{
    private readonly ISender _sender;

    public FeatureGroupsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Mevcut projeye yeni bir özellik grubu ekler.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFeatureGroupCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}