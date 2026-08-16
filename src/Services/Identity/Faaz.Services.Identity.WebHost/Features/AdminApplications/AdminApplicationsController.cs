using Faaz.Services.Identity.WebHost.Features.AdminApplications.Commands;
using Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;
using Faaz.Services.Identity.WebHost.Features.AdminApplications.Queries;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Identity.WebHost.Features.AdminApplications;

// Internal-service-key auth, mirroring InternalAdminUsersController — this is the ONLY reachable
// entry point for these actions. It used to be [Authorize(Roles = "3")] at api/v1/admin/applications,
// but the Gateway routes every /api/v1/admin/* request to the Administration service, never to
// Identity, so that route was permanently unreachable. The MediatR commands underneath (which update
// the consultant's Identity user claims, publish ConsultantApproved/Rejected/RevisionRequested events,
// and send the pre-approve invite email) were always correct — they just had no caller. Administration
// now calls this controller directly, the same way it calls every other service's internal admin API.
[Route("internal/admin/applications")]
[Tags("Internal - Admin Applications")]
[IgnoreAntiforgeryToken]
public class AdminApplicationsController : FaazApiController
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _config;

    public AdminApplicationsController(IMediator mediator, IConfiguration config)
    {
        _mediator = mediator;
        _config   = config;
    }

    /// <summary>List consultant applications with optional status filter.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ApplicationSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplications([FromQuery] string? status, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct = default)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var result = await _mediator.Send(new GetApplicationsQuery { Status = status, Page = page, PageSize = pageSize }, ct);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Get a single application in detail.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicationDetail(Guid id, CancellationToken ct)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var result = await _mediator.Send(new GetApplicationDetailQuery { ApplicationId = id }, ct);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Pre-approve an application and send the setup invite email.</summary>
    [HttpPost("{id:guid}/pre-approve")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreApprove(Guid id, [FromBody] AdminActionDto postModel, CancellationToken ct)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new PreApproveApplicationCommand { ApplicationId = id, PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("Application pre-approved. Invite email sent."));
    }

    /// <summary>Fully approve an application — activates the consultant profile.</summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] AdminActionDto postModel, CancellationToken ct)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new ApproveApplicationCommand { ApplicationId = id, PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("Application approved."));
    }

    /// <summary>Reject an application.</summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] AdminActionDto postModel, CancellationToken ct)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new RejectApplicationCommand { ApplicationId = id, PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("Application rejected."));
    }

    /// <summary>Request revisions from the consultant.</summary>
    [HttpPost("{id:guid}/request-revision")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestRevision(Guid id, [FromBody] AdminActionDto postModel, CancellationToken ct)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new RequestRevisionCommand { ApplicationId = id, PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("Revision requested."));
    }

    private bool IsInternal()
    {
        var key = HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(key) && key == _config["ServiceApiKey"];
    }
}
