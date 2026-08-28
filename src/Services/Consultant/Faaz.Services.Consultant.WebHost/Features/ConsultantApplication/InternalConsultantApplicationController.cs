using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.Commands;
using Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.DTOs;
using Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.Queries;
using Faaz.SharedKernel.Abstractions;
using Faaz.SharedKernel.Results;
using Faaz.SharedKernel.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantApplication;

[Route("api/v1/consultants")]
[Tags("Internal - Consultant Applications")]
public class InternalConsultantApplicationController : ConsultantInternalController
{
    private readonly ISender _mediator;
    private readonly IConsultantApplicationServices _appServices;

    public InternalConsultantApplicationController(ISender mediator, IConsultantApplicationServices appServices, IConfiguration config)
        : base(config)
    {
        _mediator    = mediator;
        _appServices = appServices;
    }

    [HttpPost("internal/initialise-application")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitialiseApplication([FromForm] InitialiseApplicationDto dto, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var id = await _mediator.Send(new InitialiseApplicationCommand { PostModel = dto }, ct);
        return Ok(ApiResponse.Ok(new { applicationId = id }, "Application initialised."));
    }

    [HttpPost("internal/link-user")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LinkUser([FromBody] LinkUserDto dto, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new LinkUserToApplicationCommand { PostModel = dto }, ct);
        return Ok(ApiResponse.NoContent("User linked to application."));
    }

    [HttpPost("internal/applications/user/{userId:guid}/set-under-review")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetUnderReview(Guid userId, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new SetUnderReviewCommand { UserId = userId }, ct);
        return Ok(ApiResponse.NoContent("Application moved to SettingUpProfile."));
    }

    [HttpPost("internal/applications/{applicationId:guid}/pre-approve")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreApprove(Guid applicationId, [FromBody] AdminNoteDto dto, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var inviteToken = await _mediator.Send(new PreApproveCommand { ApplicationId = applicationId, PostModel = dto }, ct);
        return Ok(ApiResponse.Ok(new { inviteToken }, "Application pre-approved."));
    }

    [HttpPost("internal/applications/{applicationId:guid}/approve")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid applicationId, [FromBody] AdminNoteDto dto, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new ApproveApplicationCommand { ApplicationId = applicationId, PostModel = dto }, ct);
        return Ok(ApiResponse.NoContent("Application approved."));
    }

    [HttpPost("internal/applications/{applicationId:guid}/reject")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(Guid applicationId, [FromBody] AdminNoteDto dto, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new RejectApplicationCommand { ApplicationId = applicationId, PostModel = dto }, ct);
        return Ok(ApiResponse.NoContent("Application rejected."));
    }

    [HttpPost("internal/applications/{applicationId:guid}/request-revision")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestRevision(Guid applicationId, [FromBody] AdminNoteDto dto, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new RequestRevisionCommand { ApplicationId = applicationId, PostModel = dto }, ct);
        return Ok(ApiResponse.NoContent("Revision requested."));
    }

    [HttpGet("internal/validate-invite")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateInvite([FromQuery] string token, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var hash = TokenHasher.Hash(token);
        var app  = await _appServices.GetByInviteTokenHashAsync(hash, ct);
        if (app is null || app.SetupInviteTokenExpiry < DateTime.UtcNow)
            return BadRequest(ApiResponse.Fail(400, "Invalid or expired invite token."));

        return Ok(ApiResponse.Ok(new { applicationId = app.Id, email = app.Email }, "Invite token valid."));
    }

    [HttpGet("internal/applications")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplications(
        [FromQuery] string? status,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken ct = default)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        ConsultantApplicationStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ConsultantApplicationStatus>(status, ignoreCase: true, out var s))
            parsedStatus = s;

        var (items, totalCount) = await _appServices.GetPagedAsync(parsedStatus, page, pageSize, ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return Ok(new
        {
            Items = items.Select(a => new
            {
                ApplicationId    = a.Id,
                a.Email,
                a.FirstName,
                a.LastName,
                a.PhoneNumber,
                a.CurrentRole,
                a.ExpertiseArea,
                a.YearsOfExperience,
                a.IsUkBased,
                Status           = a.ApplicationStatus.ToString(),
                a.SubmittedAt
            }),
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize,
            TotalPages = totalPages
        });
    }

    [HttpGet("internal/applications/{applicationId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicationDetail(
        Guid applicationId,
        [FromServices] IFileStorageService fileStorage,
        CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var a = await _appServices.GetByIdAsync(applicationId, ct);
        if (a is null) return NotFound();

        return Ok(new
        {
            ApplicationId        = a.Id,
            a.UserId,
            a.Email,
            a.FirstName,
            a.LastName,
            a.PhoneNumber,
            a.CurrentRole,
            a.Institution,
            a.ExpertiseArea,
            a.YearsOfExperience,
            a.IsUkBased,
            a.DateOfBirth,
            a.Nationality,
            a.CountryOfResidence,
            a.LinkedInProfileUrl,
            HighestQualification = a.HighestQualification?.ToString(),
            a.HighestQualificationOther,
            a.PrimaryLanguage,
            a.PersonalStatement,
            ConsultationMode     = a.ConsultationMode?.ToString(),
            a.ReferralSource,
            Status               = a.ApplicationStatus.ToString(),
            a.AdminNotes,
            a.Remarks,
            a.SubmittedAt,
            UpdatedAt            = a.UpdatedAt ?? a.SubmittedAt,
            Documents            = a.Documents.Select(d => new
            {
                d.Id,
                DocumentType = d.DocumentType.ToString(),
                d.FileName,
                d.ContentType,
                d.FileSizeBytes,
                d.UploadedAt,
                FileUrl = fileStorage.GetUrl(d.FilePath)
            })
        });
    }
}
