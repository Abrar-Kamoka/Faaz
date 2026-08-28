using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.HttpClients;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Faaz.Services.Administration.Domain.AdminEnums;

namespace Faaz.Services.Administration.WebHost.Features.Featured;

[Route("api/v1/admin/featured")]
[Authorize(Policy = "AdminOnly")]
public class FeaturedConsultantsAdminController(
    IAdminConsultantClient consultantClient,
    IAdminActionLogServices auditLog) : FaazApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await consultantClient.GetFeaturedAsync(page, pageSize, ct);
        if (result is null) return StatusCode(502, ApiResponse.Fail(502, "Consultant service unavailable."));
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("{userId:guid}/feature")]
    public async Task<IActionResult> Feature(Guid userId, CancellationToken ct = default)
    {
        var adminId = GetUserId();
        var ok      = await consultantClient.FeatureConsultantAsync(userId, adminId, ct);
        if (!ok) return StatusCode(502, ApiResponse.Fail(502, "Failed to feature consultant."));

        // No notes needed — the audit log resolves EntityId to the consultant's own display name.
        await LogAsync(adminId, AdminAction.FeatureConsultant, userId, null, ct);

        return Ok(ApiResponse.NoContent("Consultant featured."));
    }

    [HttpPost("{userId:guid}/unfeature")]
    public async Task<IActionResult> Unfeature(Guid userId, CancellationToken ct = default)
    {
        var adminId = GetUserId();
        var ok      = await consultantClient.UnfeatureConsultantAsync(userId, adminId, ct);
        if (!ok) return StatusCode(502, ApiResponse.Fail(502, "Failed to unfeature consultant."));

        await LogAsync(adminId, AdminAction.UnfeatureConsultant, userId, null, ct);

        return Ok(ApiResponse.NoContent("Consultant unfeatured."));
    }

    private async Task LogAsync(Guid adminId, AdminAction action, Guid entityId, string? notes, CancellationToken ct)
    {
        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = action,
            EntityType  = "ConsultantProfile",
            EntityId    = entityId,
            Notes       = notes,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);
    }
}
