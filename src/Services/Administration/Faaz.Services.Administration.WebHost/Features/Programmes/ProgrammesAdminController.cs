using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Faaz.Services.Administration.Domain.AdminEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Administration.WebHost.Features.Programmes;

[Route("api/v1/admin/programmes")]
[Authorize(Policy = "AdminOnly")]
public class ProgrammesAdminController(
    IProgrammeServices programmes,
    IAdminActionLogServices auditLog) : FaazApiController
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? universityId = null,
        [FromQuery] StudyLevel? studyLevel = null,
        [FromQuery] Guid? subjectId = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await programmes.GetPagedAsync(universityId, studyLevel, subjectId, search, isActive, page, pageSize, ct);
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var p = await programmes.GetByIdAsync(id, ct);
        if (p is null) return NotFound(ApiResponse.Fail(404, "Programme not found."));
        return Ok(ApiResponse.Ok(p));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertProgrammeRequest req, CancellationToken ct = default)
    {
        var adminId = GetUserId();
        var srNo    = await programmes.NewSerialNumberAsync(ct);
        var entity  = new Programme
        {
            SrNo                       = srNo,
            UniversityId               = req.UniversityId,
            Title                      = req.Title,
            StudyLevel                 = req.StudyLevel,
            Mode                       = req.Mode,
            DurationMonths             = req.DurationMonths,
            UcasCode                   = req.UcasCode,
            EntryRequirements          = req.EntryRequirements,
            TuitionFeeDomesticGbp      = req.TuitionFeeDomesticGbp,
            TuitionFeeInternationalGbp = req.TuitionFeeInternationalGbp,
            IsActive                   = true
        };
        foreach (var subjectId in req.SubjectIds ?? [])
            entity.ProgrammeSubjects.Add(new ProgrammeSubject { ProgrammeId = entity.Id, SubjectId = subjectId });

        await programmes.AddAsync(entity, ct);
        await programmes.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.CreateProgramme, entity.Id, req.Title, ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ApiResponse.Ok(new { entity.Id }));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertProgrammeRequest req, CancellationToken ct = default)
    {
        var p = await programmes.GetByIdAsync(id, ct);
        if (p is null) return NotFound(ApiResponse.Fail(404, "Programme not found."));

        var adminId                    = GetUserId();
        p.UniversityId                 = req.UniversityId;
        p.Title                        = req.Title;
        p.StudyLevel                   = req.StudyLevel;
        p.Mode                         = req.Mode;
        p.DurationMonths               = req.DurationMonths;
        p.UcasCode                     = req.UcasCode;
        p.EntryRequirements            = req.EntryRequirements;
        p.TuitionFeeDomesticGbp        = req.TuitionFeeDomesticGbp;
        p.TuitionFeeInternationalGbp   = req.TuitionFeeInternationalGbp;
        if (req.IsActive.HasValue) p.IsActive = req.IsActive.Value;

        var wanted  = (req.SubjectIds ?? []).ToHashSet();
        var current = p.ProgrammeSubjects.Select(x => x.SubjectId).ToHashSet();
        foreach (var toRemove in p.ProgrammeSubjects.Where(x => !wanted.Contains(x.SubjectId)).ToList())
            p.ProgrammeSubjects.Remove(toRemove);
        foreach (var toAdd in wanted.Where(x => !current.Contains(x)))
            p.ProgrammeSubjects.Add(new ProgrammeSubject { ProgrammeId = p.Id, SubjectId = toAdd });

        await programmes.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.UpdateProgramme, p.Id, req.Title, ct);

        return Ok(ApiResponse.NoContent("Programme updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var p = await programmes.GetByIdAsync(id, ct);
        if (p is null) return NotFound(ApiResponse.Fail(404, "Programme not found."));

        var adminId = GetUserId();
        p.IsDeleted = true;
        p.IsActive  = false;
        await programmes.SaveChangesAsync(ct);

        await LogAsync(adminId, AdminAction.DeleteProgramme, p.Id, p.Title, ct);

        return Ok(ApiResponse.NoContent("Programme deleted."));
    }

    private async Task LogAsync(Guid adminId, AdminAction action, Guid entityId, string notes, CancellationToken ct)
    {
        var srNo = await auditLog.NewSerialNumberAsync(ct);
        await auditLog.AddAsync(new AdminActionLog
        {
            SrNo        = srNo,
            AdminUserId = adminId,
            Action      = action,
            EntityType  = "Programme",
            EntityId    = entityId,
            Notes       = notes,
            PerformedAt = DateTime.UtcNow
        }, ct);
        await auditLog.SaveChangesAsync(ct);
    }
}

public record UpsertProgrammeRequest(
    Guid UniversityId,
    string Title,
    StudyLevel StudyLevel,
    ProgrammeMode Mode,
    int? DurationMonths = null,
    string? UcasCode = null,
    string? EntryRequirements = null,
    decimal? TuitionFeeDomesticGbp = null,
    decimal? TuitionFeeInternationalGbp = null,
    Guid[]? SubjectIds = null,
    bool? IsActive = null);
