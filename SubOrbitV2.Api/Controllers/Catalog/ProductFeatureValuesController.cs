using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubOrbitV2.Application.Common.Attributes;
using SubOrbitV2.Application.Features.Catalog.Commands.SetProductFeatureValue;

namespace SubOrbitV2.Api.Controllers.Catalog;

[Route("api/catalog/product-feature-values")]
[ApiController]
[Authorize]
[MustHaveProject]
public class ProductFeatureValuesController : ControllerBase
{
    private readonly ISender _sender;

    public ProductFeatureValuesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Bir ürüne ait özelliği (Feature) belirli bir değere ayarlar.
    /// Kayıt zaten varsa değerini günceller (Upsert mantığı).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SetValue([FromBody] SetProductFeatureValueCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}