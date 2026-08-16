using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Faaz.Services.Administration.Domain.AdminEnums;

namespace Faaz.Services.Administration.WebHost.Features.Universities;

[Route("api/v1/admin/universities")]
[Authorize(Policy = "AdminOnly")]
public class UniversitiesAdminController(
    IUniversityServices universities,
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
        var (items, total) = await universities.GetPagedAsync(search, isActive, page, pageSize, ct);
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var u = await universities.GetByIdAsync(id, ct);
        if (u is null) return NotFound(ApiResponse.Fail(404, "University not found."));
        return Ok(ApiResponse.Ok(u));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertUniversityRequest req, CancellationToken ct = default)
    {
        var adminId = GetUserId();
        var srNo    = await universities.NewSerialNumberAsync(ct);
        var entity  = new University
        {
            SrNo    = srNo,
            Name    = req.Name,
            Country = req.Country,
            LogoUrl = req.LogoUrl,
            IsActive = true
        };
        await universities.AddAsync(entity, ct);
        await universities.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.CreateUniversity, entity.Id, req.Name, ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ApiResponse.Ok(new { entity.Id }));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertUniversityRequest req, CancellationToken ct = default)
    {
        var u = await universities.GetByIdAsync(id, ct);
        if (u is null) return NotFound(ApiResponse.Fail(404, "University not found."));

        var adminId  = GetUserId();
        u.Name       = req.Name;
        u.Country    = req.Country;
        u.LogoUrl    = req.LogoUrl;
        if (req.IsActive.HasValue) u.IsActive = req.IsActive.Value;
        await universities.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.UpdateUniversity, u.Id, req.Name, ct);

        return Ok(ApiResponse.NoContent("University updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var u = await universities.GetByIdAsync(id, ct);
        if (u is null) return NotFound(ApiResponse.Fail(404, "University not found."));

        var adminId  = GetUserId();
        u.IsDeleted  = true;
        u.IsActive   = false;
        await universities.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.DeleteUniversity, u.Id, u.Name, ct);

        return Ok(ApiResponse.NoContent("University deleted."));
    }

    private async Task LogAsync(Guid adminId, AdminAction action, Guid entityId, string notes, CancellationToken ct)
    {
        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = action,
            EntityType  = "University",
            EntityId    = entityId,
            Notes       = notes,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);
    }
}

public record UpsertUniversityRequest(string Name, string? Country = null, string? LogoUrl = null, bool? IsActive = null);
