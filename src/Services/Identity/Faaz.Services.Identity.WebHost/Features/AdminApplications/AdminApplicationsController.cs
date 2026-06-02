using Faaz.Services.Identity.WebHost.Features.AdminApplications.Commands;
using Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;
using Faaz.Services.Identity.WebHost.Features.AdminApplications.Queries;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Identity.WebHost.Features.AdminApplications;

/// <summary>Admin management of consultant EoI applications.</summary>
[Route("api/v1/admin/applications")]
[Authorize(Roles = "3")]
[Tags("Admin - Applications")]
public class AdminApplicationsController : FaazApiController
{
    private readonly IMediator _mediator;

    public AdminApplicationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>List consultant applications with optional status filter.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ApplicationSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplications([FromQuery] string? status, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetApplicationsQuery { Status = status, Page = page, PageSize = pageSize }, ct);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Get a single application in detail.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicationDetail(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetApplicationDetailQuery { ApplicationId = id }, ct);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Pre-approve an application and send the setup invite email.</summary>
    [HttpPut("{id:guid}/pre-approve")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreApprove(Guid id, [FromBody] AdminActionDto postModel, CancellationToken ct)
    {
        await _mediator.Send(new PreApproveApplicationCommand { ApplicationId = id, PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("Application pre-approved. Invite email sent."));
    }

    /// <summary>Fully approve an application — activates the consultant profile.</summary>
    [HttpPut("{id:guid}/approve")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] AdminActionDto postModel, CancellationToken ct)
    {
        await _mediator.Send(new ApproveApplicationCommand { ApplicationId = id, PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("Application approved."));
    }

    /// <summary>Reject an application.</summary>
    [HttpPut("{id:guid}/reject")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] AdminActionDto postModel, CancellationToken ct)
    {
        await _mediator.Send(new RejectApplicationCommand { ApplicationId = id, PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("Application rejected."));
    }

    /// <summary>Request revisions from the consultant.</summary>
    [HttpPut("{id:guid}/request-revision")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestRevision(Guid id, [FromBody] AdminActionDto postModel, CancellationToken ct)
    {
        await _mediator.Send(new RequestRevisionCommand { ApplicationId = id, PostModel = postModel }, ct);
        return Ok(ApiResponse.NoContent("Revision requested."));
    }
}
