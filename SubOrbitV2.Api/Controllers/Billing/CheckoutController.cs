using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubOrbitV2.Application.Common.Attributes;
using SubOrbitV2.Application.Features.Billing.Commands.InitiateSubscription;

namespace SubOrbitV2.Api.Controllers.Billing;

[Route("api/billing/checkout")]
[ApiController]
[Authorize]
[MustHaveProject]
public class CheckoutController : ControllerBase
{
    private readonly ISender _sender;

    public CheckoutController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Bir paketi satın alma sürecini başlatır. 
    /// Taslak Payer oluşturur ve ödeme (Hosted Page) linkini döner.
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] InitiateSubscriptionCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}