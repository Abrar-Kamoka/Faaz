using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Booking.WebHost.Features.Sessions.Webhooks;

[ApiController]
[Route("api/v1/livekit")]
[Tags("LiveKit Webhooks")]
public class LiveKitWebhookController : ControllerBase
{
    private readonly ISender _mediator;

    public LiveKitWebhookController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    [Consumes("application/json", "application/webhook+json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        Request.EnableBuffering();
        using var reader = new System.IO.StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        var authHeader = Request.Headers.Authorization.FirstOrDefault() ?? "";

        await _mediator.Send(new HandleLiveKitWebhookCommand
        {
            Body                = body,
            AuthorizationHeader = authHeader
        }, ct);

        return Ok();
    }
}
