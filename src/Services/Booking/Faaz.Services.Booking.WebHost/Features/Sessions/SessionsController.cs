using Faaz.Services.Booking.WebHost.Features.Sessions.Commands;
using Faaz.Services.Booking.WebHost.Features.Sessions.DTOs;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Booking.WebHost.Features.Sessions;

[Route("api/sessions")]
public class SessionsController : FaazApiController
{
    private readonly IMediator _mediator;

    public SessionsController(IMediator mediator) { _mediator = mediator; }

    [HttpPost("{bookingId:guid}/join")]
    [Authorize(Policy = "BookingParticipantOrAdmin")]
    public async Task<IActionResult> JoinSession(Guid bookingId, [FromBody] JoinSessionDto dto)
    {
        var displayName = User.FindFirst("name")?.Value ?? User.FindFirst("sub")?.Value ?? "Participant";

        var result = await _mediator.Send(new JoinSessionCommand
        {
            BookingId        = bookingId,
            RequestingUserId = GetUserId(),
            RequestingRole   = GetRole(),
            DisplayName      = displayName,
            PostModel        = dto
        });
        return Ok(ApiResponse.Ok(result));
    }
}
