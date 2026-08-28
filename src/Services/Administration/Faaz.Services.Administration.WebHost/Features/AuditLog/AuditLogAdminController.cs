using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.AuditLog;

[Route("api/v1/admin/audit-log")]
[Authorize(Policy = "AdminOnly")]
public class AuditLogAdminController(IAdminActionLogServices auditLog, IAuditLogEnricher enricher) : FaazApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? adminId = null,
        [FromQuery] string? entityType = null,
        CancellationToken ct = default)
    {
        var (items, total) = await auditLog.GetAllAsync(page, pageSize, adminId, entityType, ct);
        var enriched = await enricher.EnrichAsync(items, ct);
        return Ok(ApiResponse.Ok(new { Items = enriched, TotalCount = total, Page = page, PageSize = pageSize }));
    }
}
