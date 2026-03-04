using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubOrbitV2.Application.Common.Attributes;
using SubOrbitV2.Application.Features.Billing.Commands.AddSubscription;

namespace SubOrbitV2.Api.Controllers.Billing;

/// <summary>
/// Abonelik yönetimi ve mevcut müşterilere eklenti (Add-on) tanımlama işlemlerini yönetir.
/// </summary>
[Route("api/billing/subscriptions")]
[ApiController]
[Authorize]
[MustHaveProject]
public class SubSubscriptionsController : ControllerBase
{
    private readonly ISender _sender;

    public SubSubscriptionsController(ISender sender)
    {
        _sender = sender;
    }
    /// <summary>
    /// Kayıtlı kartı olan mevcut bir müşteriye (Payer) yeni bir abonelik/eklenti ekler.
    /// Bu işlem müşterinin kartından anında ve otomatik tahsilat (Auto-charge) gerçekleştirir.
    /// </summary>
    /// <param name="command">PayerExternalId (Kasa), SubscriptionExternalId (Yeni Kullanıcı) ve Paket bilgileri.</param>
    [HttpPost("add-on")]
    public async Task<IActionResult> AddSubscriptionToPayer([FromBody] AddSubscriptionCommand command)
    {
        var result = await _sender.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}