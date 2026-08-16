using Faaz.Services.Administration.Domain;
using Faaz.Services.Administration.Domain.Entities;
using static Faaz.Services.Administration.Domain.AdminEnums;
using Faaz.Services.Administration.Infrastructure.HttpClients;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.Templates;

[Route("api/v1/admin/templates")]
[Authorize(Policy = "AdminOnly")]
public class TemplatesAdminController(
    IAdminNotificationClient notificationClient,
    IAdminActionLogServices auditLog) : FaazApiController
{
    [HttpGet]
    public async Task<IActionResult> GetTemplates(CancellationToken ct = default)
    {
        var result = await notificationClient.GetTemplatesAsync(ct);
        if (result is null) return Problem("Notification service unavailable", statusCode: 503);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPut("{templateId:guid}")]
    public async Task<IActionResult> UpdateTemplate(Guid templateId, [FromBody] UpdateTemplateRequest req, CancellationToken ct = default)
    {
        if (RequirePermission("content.manage") is { } denied) return denied;

        var ok = await notificationClient.UpdateTemplateAsync(templateId, req.Subject, req.Body, ct);
        if (!ok) return Problem("Failed to update template.", statusCode: 502);

        var adminId = GetUserId();
        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = AdminAction.UpdatePlatformConfig,
            EntityType  = "NotificationTemplate",
            EntityId    = templateId,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Template updated."));
    }
}

public record UpdateTemplateRequest(string Subject, string Body);
