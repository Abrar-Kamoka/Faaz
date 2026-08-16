using Faaz.Services.Administration.Domain;
using Faaz.Services.Administration.Domain.Entities;
using static Faaz.Services.Administration.Domain.AdminEnums;
using Faaz.Services.Administration.Infrastructure.HttpClients;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using Faaz.SharedKernel.Results;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.Consultants;

[Route("api/v1/admin/consultants")]
[Authorize(Policy = "AdminOnly")]
public class ConsultantsAdminController(
    IAdminConsultantClient consultantClient,
    IAdminIdentityClient identityClient,
    IAdminActionLogServices auditLog,
    IPublishEndpoint bus) : FaazApiController
{
    // ── Applications ─────────────────────────────────────────────────────────

    [HttpGet("applications")]
    public async Task<IActionResult> GetApplications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        CancellationToken ct = default)
    {
        var result = await consultantClient.GetApplicationsAsync(page, pageSize, status, ct);
        if (result is null)
            return Problem("Consultant service unavailable", statusCode: 503);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("applications/{applicationId:guid}")]
    public async Task<IActionResult> GetApplication(Guid applicationId, CancellationToken ct = default)
    {
        var result = await consultantClient.GetApplicationByIdAsync(applicationId, ct);
        if (result is null)
            return NotFound(ApiResponse.Fail(404, "Application not found"));
        return Ok(ApiResponse.Ok(result));
    }

    // Approve/Reject/RequestRevision/PreApprove are proxied through Identity, not the Consultant
    // client above — only Identity can update the consultant's Identity user claims and publish the
    // ConsultantApproved/Rejected/RevisionRequested integration events that Notification (email +
    // SignalR pop) and Consultant (profile activation) actually consume. Calling Consultant's plain
    // status-flip endpoint directly here would silently skip all of that (see PreApproveApplication
    // for the invite-email step this used to be missing entirely).

    [HttpPost("applications/{applicationId:guid}/pre-approve")]
    public async Task<IActionResult> PreApproveApplication(
        Guid applicationId,
        [FromBody] NotesRequest? req,
        CancellationToken ct = default)
    {
        var app = await consultantClient.GetApplicationByIdAsync(applicationId, ct);
        if (app is null) return NotFound(ApiResponse.Fail(404, "Application not found"));

        var adminId = GetUserId();
        var result  = await identityClient.PreApproveApplicationAsync(applicationId, app.Email, req?.Notes, ct);
        if (!result.Success)
            return UnprocessableEntity(ApiResponse.Fail(422, result.Error ?? "Failed to pre-approve application."));

        await WriteAuditAsync(adminId, AdminAction.ApproveConsultant, "ConsultantApplication", applicationId, "Pre-approved — invite sent", ct);
        return Ok(ApiResponse.NoContent("Application pre-approved. Invite email sent."));
    }

    [HttpPost("applications/{applicationId:guid}/approve")]
    public async Task<IActionResult> ApproveApplication(Guid applicationId, CancellationToken ct = default)
    {
        var app = await consultantClient.GetApplicationByIdAsync(applicationId, ct);
        if (app is null) return NotFound(ApiResponse.Fail(404, "Application not found"));

        var adminId = GetUserId();
        var result  = await identityClient.ApproveApplicationAsync(applicationId, app.Email, null, ct);
        if (!result.Success)
            return UnprocessableEntity(ApiResponse.Fail(422, result.Error ?? "Failed to approve application."));

        await WriteAuditAsync(adminId, AdminAction.ApproveConsultant, "ConsultantApplication", applicationId, null, ct);
        return Ok(ApiResponse.NoContent("Application approved."));
    }

    [HttpPost("applications/{applicationId:guid}/reject")]
    public async Task<IActionResult> RejectApplication(
        Guid applicationId,
        [FromBody] ReasonRequest req,
        CancellationToken ct = default)
    {
        var app = await consultantClient.GetApplicationByIdAsync(applicationId, ct);
        if (app is null) return NotFound(ApiResponse.Fail(404, "Application not found"));

        var adminId = GetUserId();
        var result  = await identityClient.RejectApplicationAsync(applicationId, app.Email, req.Reason, ct);
        if (!result.Success)
            return UnprocessableEntity(ApiResponse.Fail(422, result.Error ?? "Failed to reject application."));

        await WriteAuditAsync(adminId, AdminAction.RejectConsultant, "ConsultantApplication", applicationId, req.Reason, ct);
        return Ok(ApiResponse.NoContent("Application rejected."));
    }

    [HttpPost("applications/{applicationId:guid}/revision")]
    public async Task<IActionResult> RequestRevision(
        Guid applicationId,
        [FromBody] NotesRequest req,
        CancellationToken ct = default)
    {
        var app = await consultantClient.GetApplicationByIdAsync(applicationId, ct);
        if (app is null) return NotFound(ApiResponse.Fail(404, "Application not found"));

        var adminId = GetUserId();
        var result  = await identityClient.RequestRevisionApplicationAsync(applicationId, app.Email, req.Notes, ct);
        if (!result.Success)
            return UnprocessableEntity(ApiResponse.Fail(422, result.Error ?? "Failed to request revision."));

        await WriteAuditAsync(adminId, AdminAction.RequestConsultantRevision, "ConsultantApplication", applicationId, req.Notes, ct);
        return Ok(ApiResponse.NoContent("Revision requested."));
    }

    // ── Profiles ─────────────────────────────────────────────────────────────

    [HttpGet("profiles")]
    public async Task<IActionResult> GetProfiles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await consultantClient.GetProfilesAsync(page, pageSize, ct);
        if (result is null)
            return Problem("Consultant service unavailable", statusCode: 503);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("profiles/{userId:guid}")]
    public async Task<IActionResult> GetProfile(Guid userId, CancellationToken ct = default)
    {
        var result = await consultantClient.GetProfileByIdAsync(userId, ct);
        if (result is null)
            return NotFound(ApiResponse.Fail(404, "Consultant profile not found"));
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("{userId:guid}/suspend")]
    public async Task<IActionResult> SuspendConsultant(
        Guid userId,
        [FromBody] ReasonRequest req,
        CancellationToken ct = default)
    {
        if (RequirePermission("consultants.review") is { } denied) return denied;

        var adminId = GetUserId();
        var ok = await consultantClient.SuspendConsultantAsync(userId, adminId, req.Reason, ct);
        if (!ok)
            return Problem("Failed to suspend consultant", statusCode: 502);

        await WriteAuditAsync(adminId, AdminAction.SuspendConsultant, "ConsultantProfile", userId, req.Reason, ct);
        await bus.Publish(new ConsultantSuspendedByAdminEvent(userId, adminId, req.Reason), ct);

        return Ok(ApiResponse.NoContent("Consultant suspended."));
    }

    [HttpPost("{userId:guid}/restore")]
    public async Task<IActionResult> RestoreConsultant(Guid userId, CancellationToken ct = default)
    {
        if (RequirePermission("consultants.review") is { } denied) return denied;

        var adminId = GetUserId();
        var ok = await consultantClient.RestoreConsultantAsync(userId, adminId, ct);
        if (!ok)
            return Problem("Failed to restore consultant", statusCode: 502);

        await WriteAuditAsync(adminId, AdminAction.RestoreConsultant, "ConsultantProfile", userId, null, ct);
        await bus.Publish(new ConsultantRestoredByAdminEvent(userId, adminId), ct);

        return Ok(ApiResponse.NoContent("Consultant restored."));
    }

    private async Task WriteAuditAsync(
        Guid adminId, AdminAction action, string entityType, Guid entityId,
        string? notes, CancellationToken ct)
    {
        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = action,
            EntityType  = entityType,
            EntityId    = entityId,
            Notes       = notes,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);
    }
}

public record ReasonRequest(string Reason);
public record NotesRequest(string Notes);
