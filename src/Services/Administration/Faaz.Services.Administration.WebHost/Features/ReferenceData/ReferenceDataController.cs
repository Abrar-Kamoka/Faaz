using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Administration.WebHost.Features.ReferenceData;

// Read side of the catalog. Universities/Subjects/Services are [AllowAnonymous] — the public
// marketing site needs to resolve and filter by catalog names for visitors who aren't logged in
// at all. UniversityProgrammes and SubmitRequest stay behind [Authorize] (this class-level default)
// since both are wizard-only actions that already require an authenticated student/consultant.
// Every list here shares the same search+page/pageSize convention as the admin CRUD controllers
// (see ProgrammesAdminController etc.), so "pick from a long list" behaves identically everywhere.
[Route("api/v1/reference")]
[Authorize]
public class ReferenceDataController(
    IUniversityServices universities,
    IProgrammeServices programmes,
    ISubjectServices subjects,
    IServiceCatalogServices services,
    IReferenceDataRequestServices requests) : FaazApiController
{
    // Anonymous — the public marketing site (browse page, a consultant's public profile, the
    // homepage's featured consultants) resolves subject/university names for display without the
    // visitor being logged in at all. Only the wizard-only bits (programmes lookup, catalog
    // requests) stay behind [Authorize] below.
    [HttpGet("universities")]
    [AllowAnonymous]
    public async Task<IActionResult> Universities(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await universities.GetPagedAsync(search, isActive: true, page, pageSize, ct);
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpGet("universities/{id:guid}/programmes")]
    public async Task<IActionResult> UniversityProgrammes(
        Guid id,
        [FromQuery] StudyLevel? studyLevel = null,
        [FromQuery] Guid? subjectId = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await programmes.GetPagedAsync(id, studyLevel, subjectId, search, isActive: true, page, pageSize, ct);
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpGet("subjects")]
    [AllowAnonymous]
    public async Task<IActionResult> Subjects(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await subjects.GetPagedAsync(search, isActive: true, page, pageSize, ct);
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpGet("services")]
    [AllowAnonymous]
    public async Task<IActionResult> Services(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var (items, total) = await services.GetPagedAsync(search, isActive: true, page, pageSize, ct);
        return Ok(ApiResponse.Ok(new { Items = items, TotalCount = total, Page = page, PageSize = pageSize }));
    }

    [HttpPost("requests")]
    public async Task<IActionResult> SubmitRequest([FromBody] SubmitReferenceRequestRequest req, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var srNo   = await requests.NewSerialNumberAsync(ct);
        var entity = new ReferenceDataRequest
        {
            SrNo              = srNo,
            RequestedByUserId = userId,
            RequestedByRole   = GetRole(),
            EntityType        = req.EntityType,
            ProposedName      = req.ProposedName,
            Details           = req.Details,
            Status            = ReferenceRequestStatus.Pending
        };
        await requests.AddAsync(entity, ct);
        await requests.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok(new { entity.Id }));
    }

    // Satisfies the Discover Uni dataset's CC BY 4.0 licence: credit HESA + link, a link to the
    // licence itself, and a notice that the data has been filtered/restructured from the original.
    // The frontend renders this wherever HESA-sourced catalog data is shown.
    [HttpGet("attribution")]
    [AllowAnonymous]
    public IActionResult Attribution()
    {
        return Ok(ApiResponse.Ok(new
        {
            Notice = "Contains data derived from the Discover Uni dataset, published by the Higher Education " +
                     "Statistics Agency (HESA) on behalf of the Office for Students. This data has been filtered, " +
                     "restructured, and combined with other sources for use in this application.",
            Source     = "HESA — https://www.hesa.ac.uk",
            LicenceName = "Creative Commons Attribution 4.0 International (CC BY 4.0)",
            LicenceUrl  = "https://creativecommons.org/licenses/by/4.0/"
        }));
    }
}

public record SubmitReferenceRequestRequest(ReferenceEntityType EntityType, string ProposedName, string? Details = null);
