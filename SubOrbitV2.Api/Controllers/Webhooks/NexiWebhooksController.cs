using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubOrbitV2.Application.Features.Billing.Commands.ProcessNexiWebhook;

namespace SubOrbitV2.Api.Controllers.Webhooks;

[Route("api/webhooks/nexi")]
[ApiController]
[AllowAnonymous]
public class NexiWebhooksController : ControllerBase
{
    private readonly ISender _sender;

    public NexiWebhooksController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Nexi ödeme başarılı olduğunda bu endpoint'e POST isteği (Webhook) atar.
    /// Parametreleri direkt Query'den (URL'den) ve Header'dan yakalıyoruz.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleNexiWebhook([FromQuery] Guid projectId, [FromQuery] string subId)
    {
        // 1. Nexi'nin gönderdiği güvenlik token'ını Header'dan yakala
        var authHeader = Request.Headers["Authorization"].ToString();

        // 2. İşlemi Orkestra Şefine (Handler) devret
        var command = new ProcessNexiWebhookCommand(projectId, subId, authHeader);
        var result = await _sender.Send(command);

        // Not: Nexi gibi webhook sistemlerine işlem başarılı da olsa, 
        // mükerrer istek atmaması için (Retry Loop'a girmemesi için) genellikle 200 OK dönülür.
        if (result.IsSuccess)
            return Ok();

        return BadRequest(result.Message);
    }
}