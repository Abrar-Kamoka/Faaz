using Faaz.Services.Payment.WebHost.Features.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Payment.WebHost.Features.Webhooks;

[Route("webhooks")]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhookController(IMediator mediator) { _mediator = mediator; }

    [HttpPost("stripe")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        string payload;
        using (var reader = new StreamReader(HttpContext.Request.Body))
            payload = await reader.ReadToEndAsync();

        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? "";
        await _mediator.Send(new ProcessStripeWebhookCommand { Payload = payload, SignatureHeader = signature });
        return Ok();
    }
}
