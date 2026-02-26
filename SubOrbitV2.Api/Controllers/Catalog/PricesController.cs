using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubOrbitV2.Application.Common.Attributes;
using SubOrbitV2.Application.Features.Catalog.Commands.CreatePrice;

namespace SubOrbitV2.Api.Controllers.Catalog;

[Route("api/catalog/prices")]
[ApiController]
[Authorize]
[MustHaveProject]
public class PricesController : ControllerBase
{
    private readonly ISender _sender;

    public PricesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Belirtilen ürüne (Product) yeni bir fiyat (Price) planı ekler.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePriceCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}