using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.WebHost.Features.Sessions.Commands;
using Faaz.Services.Booking.WebHost.Features.Sessions.DTOs;
using Faaz.SharedKernel.Exceptions;
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
        // Last-resort fallback only — the handler resolves the real name from Identity;
        // the JWT itself carries no "name" claim, so this string is never anything but a placeholder.
        var displayName = "Participant";

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

        // Session notes are the consultant's own — private to them and admin, NOT the student
        // (see the "Private — only visible to you and the admin" copy on the notes page itself).
        // "BookingParticipantOrAdmin" only checks the caller is *some* authenticated user, not
        // that they're actually this booking's consultant — without this check, any consultant
        // (or any student) on the platform could read any other booking's private notes.
        var userId = GetUserId();
        var role   = GetRole();
        var isAdmin      = role == "3";
        var isConsultant = role == "2" && booking.ConsultantUserId == userId;
        if (!isAdmin && !isConsultant)
            throw new ForbiddenException("You do not have access to this booking's notes.");

        return Ok(ApiResponse.Ok(new SessionNotesDto { Notes = booking.SessionNotes ?? "" }));
    }

    [HttpPost("{bookingId:guid}/notes")]
    [Authorize(Policy = "ConsultantOnly")]
    public async Task<IActionResult> SaveSessionNotes(Guid bookingId, [FromBody] SaveSessionNotesDto dto, CancellationToken ct)
    {
        var booking = await _bookingServices.GetByIdAsync(bookingId, ct);
        if (booking is null) return NotFound(ApiResponse.Fail(404, "Booking not found."));

        // "ConsultantOnly" only checks the caller is *some* consultant — without this, any
        // consultant on the platform could overwrite another consultant's session notes.
        if (booking.ConsultantUserId != GetUserId())
            throw new ForbiddenException("You do not have access to this booking's notes.");

        booking.SessionNotes = dto.Notes?[..Math.Min(dto.Notes.Length, 500)] ?? "";
        await _bookingServices.SaveChangesAsync(ct);
        return Ok(ApiResponse.NoContent());
    }
}
