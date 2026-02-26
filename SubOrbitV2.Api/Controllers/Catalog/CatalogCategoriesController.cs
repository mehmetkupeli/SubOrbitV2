using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using SubOrbitV2.Application.Common.Attributes;
using SubOrbitV2.Application.Features.Catalog.Commands.CreateCatalogCategory;

namespace SubOrbitV2.Api.Controllers.Catalog;

[Route("api/catalog/categories")]
[ApiController]
[Authorize]
[MustHaveProject] // Header'da 'X-Project-Id' olmasını zorunlu kılan attribute
public class CatalogCategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public CatalogCategoriesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Aktif projeye yeni bir katalog kategorisi ekler.
    /// Header'da geçerli bir 'X-Project-Id' gönderilmelidir.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCatalogCategoryCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}