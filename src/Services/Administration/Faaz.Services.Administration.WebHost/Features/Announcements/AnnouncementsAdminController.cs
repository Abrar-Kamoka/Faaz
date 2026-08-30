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
        var result = await notificationClient.CreateAnnouncementAsync(req.Title, req.Body, req.Audience, req.ExpiresAt, adminId, ct);
        if (!result.Success) return AnnouncementFailure(result.ErrorMessage, "Failed to create announcement.");

        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = AdminAction.UpdatePlatformConfig,
            EntityType  = "Announcement",
            EntityId    = result.Data,
            Notes       = req.Title,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);

        return StatusCode(201, ApiResponse.Created(new { Id = result.Data }));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateAnnouncement(Guid id, CancellationToken ct = default)
    {
        if (RequirePermission("content.manage") is { } denied) return denied;

        var ok = await notificationClient.DeactivateAnnouncementAsync(id, ct);
        if (!ok) return Problem("Failed to deactivate announcement.", statusCode: 502);

        return Ok(ApiResponse.NoContent("Announcement deactivated."));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateAnnouncement(Guid id, CancellationToken ct = default)
    {
        if (RequirePermission("content.manage") is { } denied) return denied;

        var result = await notificationClient.ActivateAnnouncementAsync(id, ct);
        if (!result.Success) return AnnouncementFailure(result.ErrorMessage, "Failed to activate announcement.");

        return Ok(ApiResponse.NoContent("Announcement activated."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAnnouncement(Guid id, [FromBody] UpdateAnnouncementRequest req, CancellationToken ct = default)
    {
        if (RequirePermission("content.manage") is { } denied) return denied;

        var result = await notificationClient.UpdateAnnouncementAsync(id, req.Title, req.Body, req.Audience, req.ExpiresAt, ct);
        if (!result.Success) return AnnouncementFailure(result.ErrorMessage, "Failed to update announcement.");

        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = GetUserId(),
            Action      = AdminAction.UpdatePlatformConfig,
            EntityType  = "Announcement",
            EntityId    = id,
            Notes       = req.Title,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Announcement updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAnnouncement(Guid id, CancellationToken ct = default)
    {
        if (RequirePermission("content.manage") is { } denied) return denied;

        var ok = await notificationClient.DeleteAnnouncementAsync(id, ct);
        if (!ok) return Problem("Failed to delete announcement.", statusCode: 502);

        return Ok(ApiResponse.NoContent("Announcement deleted."));
    }

    // Notification rejects with a specific validation message (e.g. "Expiry date must be in the
    // future.") for a genuine business-rule violation — surface that as-is instead of the generic
    // fallback, which is reserved for the proxy call itself failing (service down, timeout, etc.).
    private IActionResult AnnouncementFailure(string? notificationMessage, string fallback) =>
        !string.IsNullOrWhiteSpace(notificationMessage)
            ? UnprocessableEntity(ApiResponse.Fail(422, notificationMessage))
            : Problem(fallback, statusCode: 502);
}

public record CreateAnnouncementRequest(string Title, string Body, int Audience, DateTime? ExpiresAt);
public record UpdateAnnouncementRequest(string Title, string Body, int Audience, DateTime? ExpiresAt);
