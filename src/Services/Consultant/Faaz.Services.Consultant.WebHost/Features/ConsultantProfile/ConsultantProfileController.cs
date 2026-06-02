using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Queries;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile;

[Route("api/v1/consultant-profiles")]
[Tags("Consultant Profiles")]
public class ConsultantProfileController : FaazApiController
{
    private readonly ISender _mediator;

    public ConsultantProfileController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{userId:guid}")]
    [Authorize(Policy = "ConsultantSetupOrActive")]
    [ProducesResponseType(typeof(ApiResponse<ConsultantProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetConsultantProfileQuery { UserId = userId }, ct);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{userId:guid}/completeness")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProfileCompletenessDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompleteness(Guid userId, CancellationToken ct)
    {
        if (!IsOwnerOrAdmin(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var result = await _mediator.Send(new GetProfileCompletenessQuery { UserId = userId }, ct);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPut("{userId:guid}/personal-info")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePersonalInfo(Guid userId, [FromBody] UpdatePersonalInfoDto dto, CancellationToken ct)
    {
        if (!IsOwner(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new UpdatePersonalInfoCommand { UserId = userId, PutModel = dto }, ct);
        return Ok(ApiResponse.NoContent("Personal info updated."));
    }

    [HttpPut("{userId:guid}/expertise")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateExpertise(Guid userId, [FromBody] UpdateExpertiseDto dto, CancellationToken ct)
    {
        if (!IsOwner(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new UpdateExpertiseCommand { UserId = userId, PutModel = dto }, ct);
        return Ok(ApiResponse.NoContent("Expertise updated."));
    }

    [HttpPut("{userId:guid}/bio-and-video")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBioAndVideo(Guid userId, [FromBody] UpdateBioAndVideoDto dto, CancellationToken ct)
    {
        if (!IsOwner(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new UpdateBioAndVideoCommand { UserId = userId, PutModel = dto }, ct);
        return Ok(ApiResponse.NoContent("Bio and video updated."));
    }

    [HttpPut("{userId:guid}/pricing")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePricing(Guid userId, [FromBody] UpdatePricingDto dto, CancellationToken ct)
    {
        if (!IsOwner(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new UpdatePricingCommand { UserId = userId, PutModel = dto }, ct);
        return Ok(ApiResponse.NoContent("Pricing updated."));
    }

    [HttpPut("{userId:guid}/availability")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAvailability(Guid userId, [FromBody] UpdateAvailabilityDto dto, CancellationToken ct)
    {
        if (!IsOwner(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new UpdateAvailabilityCommand { UserId = userId, PutModel = dto }, ct);
        return Ok(ApiResponse.NoContent("Availability updated."));
    }

    [HttpPut("{userId:guid}/call-preferences")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCallPreferences(Guid userId, [FromBody] UpdateCallPreferencesDto dto, CancellationToken ct)
    {
        if (!IsOwner(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        await _mediator.Send(new UpdateCallPreferencesCommand { UserId = userId, PutModel = dto }, ct);
        return Ok(ApiResponse.NoContent("Call preferences updated."));
    }
}

public class CreateProfileStubRequest
{
    public Guid UserId { get; set; }
}
