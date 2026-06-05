using Faaz.Services.Student.WebHost.Features.StudentProfile.Commands;
using Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;
using Faaz.Services.Student.WebHost.Features.StudentProfile.Queries;
using Faaz.SharedKernel.Abstractions;
using Faaz.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile;

/// <summary>Student profile management — onboarding wizard + profile retrieval.</summary>
[ApiController]
[Route("api/v1/students")]
[Tags("Student Profile")]
public class StudentProfileController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _config;

    public StudentProfileController(IMediator mediator, IConfiguration config)
    {
        _mediator = mediator;
        _config = config;
    }

    /// <summary>Internal: create a profile stub after student registration. Called by Identity Service only.</summary>
    [HttpPost("internal/create-profile-stub")]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProfileStub([FromBody] CreateProfileStubDto postModel, CancellationToken ct)
    {
        if (!IsInternalRequest())
            return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        await _mediator.Send(new CreateProfileStubCommand { PostModel = postModel }, ct);
        return StatusCode(201, ApiResponse.Created<object?>(null, "Student profile stub created."));
    }

    /// <summary>Get the full student profile.</summary>
    [HttpGet("{userId:guid}/profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<StudentProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProfile(Guid userId, CancellationToken ct)
    {
        if (!IsOwnerOrAdmin(userId))
            return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        //if (userId == null)
        //    return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));
        var result = await _mediator.Send(new GetStudentProfileQuery { UserId = userId }, ct);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Wizard Step A — update personal background.</summary>
    [HttpPut("{userId:guid}/profile/personal")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdatePersonal(Guid userId, [FromBody] UpdatePersonalBackgroundDto putModel, CancellationToken ct)
    {
        if (!IsOwner(userId))
            return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        await _mediator.Send(new UpdatePersonalBackgroundCommand { UserId = userId, PutModel = putModel }, ct);
        return Ok(ApiResponse.NoContent("Personal background updated."));
    }

    /// <summary>Wizard Step B — set study track and track-specific data.</summary>
    [HttpPut("{userId:guid}/profile/study-level")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStudyLevel(Guid userId, [FromBody] UpdateStudyLevelDto putModel, CancellationToken ct)
    {
        if (!IsOwner(userId))
            return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        await _mediator.Send(new UpdateStudyLevelCommand { UserId = userId, PutModel = putModel }, ct);
        return Ok(ApiResponse.NoContent("Study level updated."));
    }

    /// <summary>Wizard Step C — set goals, target subjects, universities, and help types.</summary>
    [HttpPut("{userId:guid}/profile/goals")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateGoals(Guid userId, [FromBody] UpdateGoalsDto putModel, CancellationToken ct)
    {
        if (!IsOwner(userId))
            return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        await _mediator.Send(new UpdateGoalsCommand { UserId = userId, PutModel = putModel }, ct);
        return Ok(ApiResponse.NoContent("Goals updated."));
    }

    /// <summary>Wizard Step D — add bio and profile photo URL.</summary>
    [HttpPut("{userId:guid}/profile/bio")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateBio(Guid userId, [FromBody] UpdateProfileBioDto putModel, CancellationToken ct)
    {
        if (!IsOwner(userId))
            return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        await _mediator.Send(new UpdateProfileBioCommand { UserId = userId, PutModel = putModel }, ct);
        return Ok(ApiResponse.NoContent("Bio updated."));
    }

    /// <summary>Get profile completeness percentage and step breakdown.</summary>
    [HttpGet("{userId:guid}/profile/completeness")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProfileCompletenessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCompleteness(Guid userId, CancellationToken ct)
    {
        if (!IsOwnerOrAdmin(userId))
            return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var result = await _mediator.Send(new GetProfileCompletenessQuery { UserId = userId }, ct);
        return Ok(ApiResponse.Ok(result));
    }

    /// <summary>Upload or replace the student's profile photo.</summary>
    [HttpPut("{userId:guid}/photo")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdatePhoto(Guid userId, IFormFile photo, [FromServices] IFileStorageService fileStorage, CancellationToken ct)
    {
        if (!IsOwner(userId))
            return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        if (photo.Length > 5 * 1024 * 1024)
            return BadRequest(ApiResponse.Fail(400, "File must not exceed 5MB."));

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(photo.ContentType))
            return BadRequest(ApiResponse.Fail(400, "Only JPEG, PNG, or WebP images are accepted."));

        await using var stream = photo.OpenReadStream();
        var storedPath = await fileStorage.UploadAsync(stream, photo.FileName, FileCategory.Profiles, ct);

        await _mediator.Send(new UpdateStudentPhotoCommand { UserId = userId, PhotoUrl = fileStorage.GetUrl(storedPath) }, ct);
        return Ok(ApiResponse.NoContent("Photo updated."));
    }

    private bool IsOwner(Guid userId)
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return sub is not null && Guid.TryParse(sub, out var id) && id == userId;
    }

    private bool IsOwnerOrAdmin(Guid userId)
    {
        if (IsOwner(userId)) return true;
        var role = User.FindFirstValue("role");
        return role == "3";
    }

    private bool IsInternalRequest()
    {
        var serviceKey = HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        var expected = _config["ServiceApiKey"];
        return !string.IsNullOrWhiteSpace(serviceKey) && serviceKey == expected;
    }
}
