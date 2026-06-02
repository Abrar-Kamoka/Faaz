using Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.Queries;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantApplication;

[Route("api/v1/consultants")]
[Tags("Consultant Applications")]
public class ConsultantApplicationController : FaazApiController
{
    private readonly ISender _mediator;

    public ConsultantApplicationController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userId:guid}/application")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyApplicationStatus(Guid userId, CancellationToken ct)
    {
        if (!IsOwnerOrAdmin(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var result = await _mediator.Send(new GetApplicationStatusQuery { UserId = userId }, ct);
        return Ok(ApiResponse.Ok(result));
    }
}
