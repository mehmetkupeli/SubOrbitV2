using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using SubOrbitV2.Application.Common.Attributes;
using SubOrbitV2.Application.Features.Catalog.Commands.CreateCoupon;

namespace SubOrbitV2.Api.Controllers.Catalog;

[Route("api/catalog/coupons")]
[ApiController]
[Authorize]
[MustHaveProject]
public class CouponsController : ControllerBase
{
    private readonly ISender _sender;

    public CouponsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Mevcut projeye yeni bir indirim kuponu tanımlar.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCouponCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}