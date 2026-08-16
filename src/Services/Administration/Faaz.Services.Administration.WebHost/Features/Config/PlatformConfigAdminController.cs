using Faaz.Services.Administration.Domain;
using Faaz.Services.Administration.Domain.Entities;
using static Faaz.Services.Administration.Domain.AdminEnums;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Administration.WebHost.Features.Config;

[Route("api/v1/admin/config")]
[Authorize(Policy = "AdminOnly")]
public class PlatformConfigAdminController(
    IPlatformConfigServices configServices,
    IAdminActionLogServices auditLog) : FaazApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var configs = await configServices.GetAllAsync(ct);
        return Ok(ApiResponse.Ok(configs));
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key, CancellationToken ct = default)
    {
        var config = await configServices.GetByKeyAsync(key, ct);
        if (config is null)
            return NotFound(ApiResponse.Fail(404, $"Config key '{key}' not found"));
        return Ok(ApiResponse.Ok(config));
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Upsert(
        string key,
        [FromBody] UpsertConfigRequest req,
        CancellationToken ct = default)
    {
        var adminId = GetUserId();
        await configServices.UpsertAsync(key, req.Value, req.Description, adminId, ct);
        await configServices.SaveChangesAsync(ct);

        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = AdminAction.UpdatePlatformConfig,
            EntityType  = "PlatformConfig",
            EntityId    = Guid.Empty,
            Notes       = $"Key={key} Value={req.Value}",
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Config updated."));
    }
}

public record UpsertConfigRequest(string Value, string? Description = null);
