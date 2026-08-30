using Faaz.Services.Consultant.Domain.Entities;
using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.SharedKernel.Abstractions;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile;

[Route("internal/admin/consultants")]
[Tags("Internal - Admin Consultants")]
[IgnoreAntiforgeryToken]
public class InternalAdminConsultantsController : ConsultantInternalController
{
    private readonly IConsultantApplicationServices _appServices;
    private readonly IConsultantProfileServices _profileServices;
    private readonly IFileStorageService _fileStorage;

    public InternalAdminConsultantsController(
        IConsultantApplicationServices appServices,
        IConsultantProfileServices profileServices,
        IFileStorageService fileStorage,
        IConfiguration config) : base(config)
    {
        _appServices     = appServices;
        _profileServices = profileServices;
        _fileStorage     = fileStorage;
    }

    // Faaz has no single "hourly rate" concept — consultants price per session type (e.g. a 30-min
    // Discovery Call at a flat fee), not by the hour. This normalizes their active session types to
    // a £/hour equivalent so the admin list has a single comparable figure, rather than the literal
    // 0m stub this used to return regardless of what the consultant actually charges.
    private static decimal ComputeHourlyRateGbp(IEnumerable<ConsultantSessionType> sessionTypes)
    {
        var active = sessionTypes.Where(s => s.IsActive && s.DurationMinutes > 0).ToList();
        if (active.Count == 0) return 0m;
        return Math.Round(active.Average(s => s.PriceGbp / s.DurationMinutes * 60m), 2);
    }

    // ── Applications ─────────────────────────────────────────────────────────

    [HttpGet("applications")]
    public async Task<IActionResult> GetApplications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        ConsultantApplicationStatus? parsedStatus = status.HasValue
            ? (ConsultantApplicationStatus)status.Value
            : null;

        var (items, total) = await _appServices.GetPagedAsync(parsedStatus, page, pageSize, ct);
        return Ok(ApiResponse.Ok(new
        {
            Items = items.Select(a => new
            {
                Id                = a.Id,
                a.Email,
                a.FirstName,
                a.LastName,
                ApplicationStatus = (int)a.ApplicationStatus,
                a.SubmittedAt,
                a.LinkedInProfileUrl,
                a.ExpertiseArea,
                a.YearsOfExperience
            }),
            TotalCount = total
        }));
    }

    [HttpGet("applications/{applicationId:guid}")]
    public async Task<IActionResult> GetApplicationDetail(Guid applicationId, CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var a = await _appServices.GetByIdAsync(applicationId, ct);
        if (a is null) return NotFound(ApiResponse.Fail(404, "Application not found."));

        // GetByIdAsync already .Include()s Documents — this previously hardcoded EoiDocumentUrl to
        // null and dropped most of the entity, leaving admins reviewing an application with almost
        // nothing to actually base an approve/reject/revision decision on.
        return Ok(ApiResponse.Ok(new
        {
            Id                    = a.Id,
            a.UserId,
            a.Email,
            a.FirstName,
            a.LastName,
            a.PhoneNumber,
            a.IsUkBased,
            a.CurrentRole,
            a.ExpertiseArea,
            a.YearsOfExperience,
            a.DateOfBirth,
            a.Nationality,
            a.CountryOfResidence,
            a.LinkedInProfileUrl,
            HighestQualification  = a.HighestQualification?.ToString(),
            a.PrimaryLanguage,
            a.PersonalStatement,
            ConsultationMode      = a.ConsultationMode?.ToString(),
            ApplicationStatus     = (int)a.ApplicationStatus,
            a.AdminNotes,
            a.SetupInviteSentAt,
            a.SubmittedAt,
            Documents = a.Documents.Select(d => new
            {
                d.Id,
                DocumentType = d.DocumentType.ToString(),
                d.FileName,
                Url = _fileStorage.GetUrl(d.FilePath),
                d.UploadedAt
            })
        }));
    }

    [HttpPost("applications/{applicationId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid applicationId, [FromBody] AdminBody req, CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var app = await _appServices.GetByIdAsync(applicationId, ct);
        if (app is null) return NotFound(ApiResponse.Fail(404, "Application not found."));

        app.ApplicationStatus = ConsultantApplicationStatus.Active;
        app.AdminNotes        = null;
        app.UpdatedAt         = DateTime.UtcNow;
        await _appServices.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Application approved."));
    }

    [HttpPost("applications/{applicationId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid applicationId, [FromBody] AdminBodyWithReason req, CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var app = await _appServices.GetByIdAsync(applicationId, ct);
        if (app is null) return NotFound(ApiResponse.Fail(404, "Application not found."));

        app.ApplicationStatus = ConsultantApplicationStatus.Rejected;
        app.AdminNotes        = req.Reason;
        app.UpdatedAt         = DateTime.UtcNow;
        await _appServices.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Application rejected."));
    }

    [HttpPost("applications/{applicationId:guid}/revision")]
    public async Task<IActionResult> RequestRevision(Guid applicationId, [FromBody] AdminBodyWithNotes req, CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var app = await _appServices.GetByIdAsync(applicationId, ct);
        if (app is null) return NotFound(ApiResponse.Fail(404, "Application not found."));

        app.ApplicationStatus = ConsultantApplicationStatus.PendingRevision;
        app.AdminNotes        = req.Notes;
        app.UpdatedAt         = DateTime.UtcNow;
        await _appServices.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Revision requested."));
    }

    // ── Profiles ─────────────────────────────────────────────────────────────

    [HttpGet("profiles")]
    public async Task<IActionResult> GetProfiles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var (items, total) = await _profileServices.GetAllForAdminAsync(page, pageSize, ct);
        return Ok(ApiResponse.Ok(new
        {
            Items = items.Select(p => new
            {
                UserId            = p.UserId,
                p.FullLegalName,
                p.WrittenBio,
                p.ProfessionalPhotoUrl,
                Email             = p.Application?.Email,
                ApplicationStatus = p.Application is not null ? (int)p.Application.ApplicationStatus : 0,
                p.IsActive,
                HourlyRateGbp     = ComputeHourlyRateGbp(p.SessionTypes)
            }),
            TotalCount = total
        }));
    }

    [HttpGet("profiles/{userId:guid}")]
    public async Task<IActionResult> GetProfileDetail(Guid userId, CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var p = await _profileServices.GetByUserIdWithApplicationAsync(userId, ct);
        if (p is null) return NotFound(ApiResponse.Fail(404, "Profile not found."));

        return Ok(ApiResponse.Ok(new
        {
            UserId            = p.UserId,
            p.FullLegalName,
            p.WrittenBio,
            p.ProfessionalPhotoUrl,
            Email             = p.Application?.Email,
            ApplicationStatus = p.Application is not null ? (int)p.Application.ApplicationStatus : 0,
            p.IsActive,
            HourlyRateGbp     = ComputeHourlyRateGbp(p.SessionTypes),
            p.CurrentRole,
            p.Institution,
            p.YearsOfExperience,
            SubjectIds = p.Subjects.Select(s => s.SubjectId),
            SessionTypes = p.SessionTypes
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .Select(s => new { s.Name, s.DurationMinutes, s.PriceGbp })
        }));
    }

    [HttpPost("{userId:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid userId, [FromBody] AdminBodyWithReason req, CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var profile = await _profileServices.GetByUserIdWithApplicationAsync(userId, ct);
        if (profile is null) return NotFound(ApiResponse.Fail(404, "Profile not found."));

        profile.IsActive = false;
        if (profile.Application is not null)
        {
            profile.Application.ApplicationStatus = ConsultantApplicationStatus.Suspended;
            profile.Application.AdminNotes        = req.Reason;
            profile.Application.UpdatedAt         = DateTime.UtcNow;
        }
        await _profileServices.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Consultant suspended."));
    }

    [HttpPost("{userId:guid}/restore")]
    public async Task<IActionResult> Restore(Guid userId, [FromBody] AdminBody req, CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var profile = await _profileServices.GetByUserIdWithApplicationAsync(userId, ct);
        if (profile is null) return NotFound(ApiResponse.Fail(404, "Profile not found."));

        profile.IsActive = true;
        if (profile.Application is not null)
        {
            profile.Application.ApplicationStatus = ConsultantApplicationStatus.Active;
            profile.Application.UpdatedAt         = DateTime.UtcNow;
        }
        await _profileServices.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Consultant restored."));
    }

    // ── Featured ──────────────────────────────────────────────────────────────

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var (items, total) = await _profileServices.GetFeaturedAsync(page, pageSize, ct);
        return Ok(ApiResponse.Ok(new
        {
            Items = items.Select(p => new
            {
                UserId            = p.UserId,
                p.FullLegalName,
                p.WrittenBio,
                p.ProfessionalPhotoUrl,
                Email             = p.Application?.Email,
                ApplicationStatus = p.Application is not null ? (int)p.Application.ApplicationStatus : 0,
                p.IsActive,
                p.IsFeatured,
                HourlyRateGbp     = 0m
            }),
            TotalCount = total
        }));
    }

    [HttpPost("{userId:guid}/feature")]
    public async Task<IActionResult> Feature(Guid userId, [FromBody] AdminBody req, CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var profile = await _profileServices.GetByUserIdWithApplicationAsync(userId, ct);
        if (profile is null) return NotFound(ApiResponse.Fail(404, "Profile not found."));

        profile.IsFeatured = true;
        await _profileServices.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Consultant featured."));
    }

    [HttpPost("{userId:guid}/unfeature")]
    public async Task<IActionResult> Unfeature(Guid userId, [FromBody] AdminBody req, CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var profile = await _profileServices.GetByUserIdWithApplicationAsync(userId, ct);
        if (profile is null) return NotFound(ApiResponse.Fail(404, "Profile not found."));

        profile.IsFeatured = false;
        await _profileServices.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Consultant unfeatured."));
    }
}

public record AdminBody(Guid AdminId);
public record AdminBodyWithReason(Guid AdminId, string Reason);
public record AdminBodyWithNotes(Guid AdminId, string Notes);
