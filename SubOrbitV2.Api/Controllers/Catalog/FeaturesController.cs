using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubOrbitV2.Application.Common.Attributes;
using SubOrbitV2.Application.Features.Catalog.Commands.CreateFeature;

namespace SubOrbitV2.Api.Controllers.Catalog;

[Route("api/catalog/features")]
[ApiController]
[Authorize]
[MustHaveProject]
public class FeaturesController : ControllerBase
{
    private readonly ISender _sender;

    public FeaturesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFeatureCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}