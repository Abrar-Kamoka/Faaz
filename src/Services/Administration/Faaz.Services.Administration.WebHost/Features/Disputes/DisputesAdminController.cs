using Faaz.Services.Administration.Domain;
using Faaz.Services.Administration.Domain.Entities;
using static Faaz.Services.Administration.Domain.AdminEnums;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.Disputes;

[Route("api/v1/admin/disputes")]
[Authorize(Policy = "AdminOnly")]
public class DisputesAdminController(
    IDisputeNoteServices disputeNotes,
    IAdminActionLogServices auditLog) : FaazApiController
{
    [HttpGet("{bookingId:guid}/notes")]
    public async Task<IActionResult> GetNotes(Guid bookingId, CancellationToken ct = default)
    {
        var notes = await disputeNotes.GetByBookingIdAsync(bookingId, ct);
        return Ok(ApiResponse.Ok(notes));
    }

    [HttpPost("{bookingId:guid}/notes")]
    public async Task<IActionResult> AddNote(
        Guid bookingId,
        [FromBody] AddNoteRequest req,
        CancellationToken ct = default)
    {
        var adminId = GetUserId();

        var srNo = await disputeNotes.NewSerialNumberAsync(ct);
        var note = new DisputeNote
        {
            SrNo          = srNo,
            BookingId     = bookingId,
            AuthorAdminId = adminId,
            Content       = req.Content,
            CreatedAt     = DateTime.UtcNow
        };
        await disputeNotes.AddAsync(note, ct);
        await disputeNotes.SaveChangesAsync(ct);

        var logSrNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = logSrNo,
            AdminUserId = adminId,
            Action      = AdminAction.AddDisputeNote,
            EntityType  = "Booking",
            EntityId    = bookingId,
            Notes       = $"Note added: {req.Content[..Math.Min(100, req.Content.Length)]}",
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);

        return StatusCode(201, ApiResponse.Created(new { note.Id }));
    }
}

public record AddNoteRequest(string Content);
