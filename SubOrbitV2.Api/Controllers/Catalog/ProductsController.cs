using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubOrbitV2.Application.Common.Attributes;
using SubOrbitV2.Application.Features.Catalog.Commands.CreateProduct;

namespace SubOrbitV2.Api.Controllers.Catalog;

[Route("api/catalog/products")]
[ApiController]
[Authorize]
[MustHaveProject]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Mevcut projeye yeni bir ürün paketi ekler.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}