using Faaz.Services.Administration.Domain;
using Faaz.Services.Administration.Domain.Entities;
using static Faaz.Services.Administration.Domain.AdminEnums;
using Faaz.Services.Administration.Infrastructure.HttpClients;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.Announcements;

[Route("api/v1/admin/announcements")]
[Authorize(Policy = "AdminOnly")]
public class AnnouncementsAdminController(
    IAdminNotificationClient notificationClient,
    IAdminActionLogServices auditLog) : FaazApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAnnouncements(CancellationToken ct = default)
    {
        var result = await notificationClient.GetAnnouncementsAsync(ct);
        if (result is null) return Problem("Notification service unavailable", statusCode: 503);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementRequest req, CancellationToken ct = default)
    {
        if (RequirePermission("content.manage") is { } denied) return denied;

        var adminId = GetUserId();
        var id = await notificationClient.CreateAnnouncementAsync(req.Title, req.Body, req.Audience, req.ExpiresAt, adminId, ct);
        if (id is null) return Problem("Failed to create announcement.", statusCode: 502);

        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = AdminAction.UpdatePlatformConfig,
            EntityType  = "Announcement",
            EntityId    = id.Value,
            Notes       = req.Title,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);

        return StatusCode(201, ApiResponse.Created(new { Id = id }));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateAnnouncement(Guid id, CancellationToken ct = default)
    {
        if (RequirePermission("content.manage") is { } denied) return denied;

        var ok = await notificationClient.DeactivateAnnouncementAsync(id, ct);
        if (!ok) return Problem("Failed to deactivate announcement.", statusCode: 502);

        return Ok(ApiResponse.NoContent("Announcement deactivated."));
    }
}

public record CreateAnnouncementRequest(string Title, string Body, int Audience, DateTime? ExpiresAt);
