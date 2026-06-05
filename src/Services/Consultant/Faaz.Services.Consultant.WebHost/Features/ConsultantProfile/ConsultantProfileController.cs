using Faaz.Services.Consultant.Domain.Entities;
using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Commands;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.Queries;
using Faaz.SharedKernel.Abstractions;
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

    [HttpPut("{userId:guid}/photo")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePhoto(
        Guid userId,
        IFormFile photo,
        [FromServices] IFileStorageService fileStorage,
        CancellationToken ct)
    {
        if (!IsOwner(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        if (photo.Length > 5 * 1024 * 1024)
            return BadRequest(ApiResponse.Fail(400, "File must not exceed 5MB."));

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(photo.ContentType))
            return BadRequest(ApiResponse.Fail(400, "Only JPEG, PNG, or WebP images are accepted."));

        await using var stream = photo.OpenReadStream();
        var storedPath = await fileStorage.UploadAsync(stream, photo.FileName, FileCategory.Profiles, ct);

        await _mediator.Send(new UpdateConsultantPhotoCommand { UserId = userId, PhotoUrl = fileStorage.GetUrl(storedPath) }, ct);
        return Ok(ApiResponse.NoContent("Photo updated."));
    }

    [HttpPost("{userId:guid}/credentials")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [ProducesResponseType(typeof(ApiResponse<ConsultantCredentialDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadCredential(
        Guid userId,
        IFormFile file,
        [FromServices] IFileStorageService fileStorage,
        [FromServices] IConsultantProfileServices profileServices,
        [FromServices] IConsultantCredentialServices credentialServices,
        CancellationToken ct)
    {
        if (!IsOwner(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(ApiResponse.Fail(400, "File must not exceed 10MB."));

        var allowed = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(ApiResponse.Fail(400, "Only PDF, JPEG, or PNG files are accepted."));

        var profile = await profileServices.GetByUserIdAsync(userId, ct);
        if (profile is null)
            return NotFound(ApiResponse.Fail(404, "Consultant profile not found."));

        await using var stream = file.OpenReadStream();
        var storedPath = await fileStorage.UploadAsync(stream, file.FileName, FileCategory.Credentials, ct);

        var credential = new ConsultantCredential
        {
            ConsultantProfileId = profile.Id,
            FileName            = file.FileName,
            StoredPath          = storedPath,
            ContentType         = file.ContentType,
            FileSizeBytes       = file.Length,
            UploadedAt          = DateTime.UtcNow
        };

        await credentialServices.AddAsync(credential, ct);
        await credentialServices.SaveChangesAsync(ct);

        var dto = new ConsultantCredentialDto
        {
            Id            = credential.Id,
            FileName      = credential.FileName,
            Url           = fileStorage.GetUrl(storedPath),
            ContentType   = credential.ContentType,
            FileSizeBytes = credential.FileSizeBytes,
            UploadedAt    = credential.UploadedAt
        };

        return StatusCode(201, ApiResponse.Created(dto, "Credential uploaded."));
    }

    [HttpDelete("{userId:guid}/credentials/{credentialId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCredential(
        Guid userId,
        Guid credentialId,
        [FromServices] IConsultantCredentialServices credentialServices,
        CancellationToken ct)
    {
        if (!IsOwner(userId)) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var credential = await credentialServices.GetByIdAsync(credentialId, ct);
        if (credential is null)
            return NotFound(ApiResponse.Fail(404, "Credential not found."));

        credentialServices.Delete(credential);
        await credentialServices.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Credential deleted."));
    }
}

public class CreateProfileStubRequest
{
    public Guid UserId { get; set; }
}
