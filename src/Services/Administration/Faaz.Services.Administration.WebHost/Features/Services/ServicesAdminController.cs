using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Faaz.Services.Administration.Domain.AdminEnums;

namespace Faaz.Services.Administration.WebHost.Features.Services;

[Route("api/v1/admin/services")]
[Authorize(Policy = "AdminOnly")]
public class ServicesAdminController(
    IServiceCatalogServices services,
    IAdminActionLogServices auditLog) : FaazApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await services.GetPagedAsync(search, isActive, page, pageSize, ct);
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var s = await services.GetByIdAsync(id, ct);
        if (s is null) return NotFound(ApiResponse.Fail(404, "Service not found."));
        return Ok(ApiResponse.Ok(s));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertServiceRequest req, CancellationToken ct = default)
    {
        var adminId = GetUserId();
        var srNo    = await services.NewSerialNumberAsync(ct);
        var entity  = new Service
        {
            SrNo        = srNo,
            Name        = req.Name,
            Description = req.Description,
            Category    = req.Category,
            SortOrder   = req.SortOrder ?? 0,
            IsActive    = true
        };
        await services.AddAsync(entity, ct);
        await services.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.CreateService, entity.Id, req.Name, ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ApiResponse.Ok(new { entity.Id }));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertServiceRequest req, CancellationToken ct = default)
    {
        var s = await services.GetByIdAsync(id, ct);
        if (s is null) return NotFound(ApiResponse.Fail(404, "Service not found."));

        var adminId   = GetUserId();
        s.Name        = req.Name;
        s.Description = req.Description;
        s.Category    = req.Category;
        if (req.SortOrder.HasValue) s.SortOrder = req.SortOrder.Value;
        if (req.IsActive.HasValue) s.IsActive = req.IsActive.Value;
        await services.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.UpdateService, s.Id, req.Name, ct);

        return Ok(ApiResponse.NoContent("Service updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var s = await services.GetByIdAsync(id, ct);
        if (s is null) return NotFound(ApiResponse.Fail(404, "Service not found."));

        var adminId = GetUserId();
        s.IsDeleted = true;
        s.IsActive  = false;
        await services.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.DeleteService, s.Id, s.Name, ct);

        return Ok(ApiResponse.NoContent("Service deleted."));
    }

    private async Task LogAsync(Guid adminId, AdminAction action, Guid entityId, string notes, CancellationToken ct)
    {
        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = action,
            EntityType  = "Service",
            EntityId    = entityId,
            Notes       = notes,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);
    }
}

public record UpsertServiceRequest(string Name, string? Description = null, string? Category = null, int? SortOrder = null, bool? IsActive = null);
