using Faaz.Services.Student.WebHost.Features.SavedConsultants.Commands;
using Faaz.Services.Student.WebHost.Features.SavedConsultants.Queries;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Faaz.Services.Student.WebHost.Features.SavedConsultants;

[ApiController]
[Route("api/v1/students")]
[Tags("Saved Consultants")]
public class SavedConsultantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SavedConsultantsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet("{userId:guid}/saved-consultants")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(Guid userId, CancellationToken ct)
    {
        if (!IsOwner(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var result = await _mediator.Send(new GetSavedConsultantsQuery { StudentUserId = userId }, ct);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("{userId:guid}/saved-consultants")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Save(Guid userId, [FromBody] SaveConsultantDto dto, CancellationToken ct)
    {
        if (!IsOwner(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        await _mediator.Send(new SaveConsultantCommand { StudentUserId = userId, ConsultantUserId = dto.ConsultantUserId }, ct);
        return Ok(ApiResponse.NoContent("Consultant saved."));
    }

    [HttpDelete("{userId:guid}/saved-consultants/{consultantUserId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Unsave(Guid userId, Guid consultantUserId, CancellationToken ct)
    {
        if (!IsOwner(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        await _mediator.Send(new UnsaveConsultantCommand { StudentUserId = userId, ConsultantUserId = consultantUserId }, ct);
        return Ok(ApiResponse.NoContent("Consultant unsaved."));
    }

    private bool IsOwner(Guid userId)
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return sub is not null && Guid.TryParse(sub, out var id) && id == userId;
    }

    public record SaveConsultantDto(Guid ConsultantUserId);
}
