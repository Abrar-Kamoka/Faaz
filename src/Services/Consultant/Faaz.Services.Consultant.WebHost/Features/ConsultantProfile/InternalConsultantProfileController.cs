using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Queries;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile;

[Route("api/v1/consultant-profiles")]
[Tags("Internal - Consultant Profiles")]
public class InternalConsultantProfileController : ConsultantInternalController
{
    private readonly ISender _mediator;

    public InternalConsultantProfileController(ISender mediator, IConfiguration config)
        : base(config)
    {
        _mediator = mediator;
    }

    [HttpPost("internal/create-stub")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateStub([FromBody] CreateProfileStubRequest dto, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new CreateProfileStubCommand { UserId = dto.UserId }, ct);
        return Ok(ApiResponse.NoContent("Consultant profile stub created."));
    }

    [HttpGet("internal/{userId:guid}/is-complete")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIsComplete(Guid userId, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var result = await _mediator.Send(new GetProfileCompletenessQuery { UserId = userId }, ct);
        return Ok(new { isComplete = result.IsComplete });
    }

    [HttpPost("internal/{userId:guid}/activate")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActivateProfile(Guid userId, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new ActivateProfileCommand { UserId = userId }, ct);
        return Ok(ApiResponse.NoContent("Consultant profile activated."));
    }
}
