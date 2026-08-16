using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Faaz.Services.Administration.Domain.AdminEnums;

namespace Faaz.Services.Administration.WebHost.Features.Subjects;

[Route("api/v1/admin/subjects")]
[Authorize(Policy = "AdminOnly")]
public class SubjectsAdminController(
    ISubjectServices subjects,
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
        var (items, total) = await subjects.GetPagedAsync(search, isActive, page, pageSize, ct);
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var s = await subjects.GetByIdAsync(id, ct);
        if (s is null) return NotFound(ApiResponse.Fail(404, "Subject not found."));
        return Ok(ApiResponse.Ok(s));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertSubjectRequest req, CancellationToken ct = default)
    {
        var adminId = GetUserId();
        var srNo    = await subjects.NewSerialNumberAsync(ct);
        var entity  = new Subject
        {
            SrNo     = srNo,
            Name     = req.Name,
            Category = req.Category,
            IsActive = true
        };
        await subjects.AddAsync(entity, ct);
        await subjects.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.CreateSubject, entity.Id, req.Name, ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ApiResponse.Ok(new { entity.Id }));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertSubjectRequest req, CancellationToken ct = default)
    {
        var s = await subjects.GetByIdAsync(id, ct);
        if (s is null) return NotFound(ApiResponse.Fail(404, "Subject not found."));

        var adminId   = GetUserId();
        s.Name        = req.Name;
        s.Category    = req.Category;
        if (req.IsActive.HasValue) s.IsActive = req.IsActive.Value;
        await subjects.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.UpdateSubject, s.Id, req.Name, ct);

        return Ok(ApiResponse.NoContent("Subject updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var s = await subjects.GetByIdAsync(id, ct);
        if (s is null) return NotFound(ApiResponse.Fail(404, "Subject not found."));

        var adminId = GetUserId();
        s.IsDeleted = true;
        s.IsActive  = false;
        await subjects.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.DeleteSubject, s.Id, s.Name, ct);

        return Ok(ApiResponse.NoContent("Subject deleted."));
    }

    private async Task LogAsync(Guid adminId, AdminAction action, Guid entityId, string notes, CancellationToken ct)
    {
        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = action,
            EntityType  = "Subject",
            EntityId    = entityId,
            Notes       = notes,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);
    }
}

public record UpsertSubjectRequest(string Name, string? Category = null, bool? IsActive = null);
