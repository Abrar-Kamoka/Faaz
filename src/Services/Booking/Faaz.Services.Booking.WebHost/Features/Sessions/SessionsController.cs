using Faaz.Services.Booking.Infrastructure.Interfaces;
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
    private readonly IBookingServices _bookingServices;

    public SessionsController(IMediator mediator, IBookingServices bookingServices)
    {
        _mediator        = mediator;
        _bookingServices = bookingServices;
    }

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

    [HttpGet("{bookingId:guid}/notes")]
    [Authorize(Policy = "BookingParticipantOrAdmin")]
    public async Task<IActionResult> GetSessionNotes(Guid bookingId, CancellationToken ct)
    {
        var booking = await _bookingServices.GetByIdAsync(bookingId, ct);
        if (booking is null) return NotFound(ApiResponse.Fail(404, "Booking not found."));
        return Ok(ApiResponse.Ok(new SessionNotesDto { Notes = booking.SessionNotes ?? "" }));
    }

    [HttpPost("{bookingId:guid}/notes")]
    [Authorize(Policy = "ConsultantOnly")]
    public async Task<IActionResult> SaveSessionNotes(Guid bookingId, [FromBody] SaveSessionNotesDto dto, CancellationToken ct)
    {
        var booking = await _bookingServices.GetByIdAsync(bookingId, ct);
        if (booking is null) return NotFound(ApiResponse.Fail(404, "Booking not found."));
        booking.SessionNotes = dto.Notes?[..Math.Min(dto.Notes.Length, 500)] ?? "";
        await _bookingServices.SaveChangesAsync(ct);
        return Ok(ApiResponse.NoContent());
    }
}
