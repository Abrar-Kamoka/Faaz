using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Faaz.Services.Identity.Domain.Entities;

namespace Faaz.Services.Identity.WebHost.Features.Users;

[Route("api/v1/users")]
[Tags("Internal - Users")]
public class InternalUsersController : FaazApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public InternalUsersController(UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config      = config;
    }

    [HttpPut("internal/{userId:guid}/name")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateName(Guid userId, [FromBody] UpdateUserNameDto dto, CancellationToken ct)
    {
        if (!IsInternalRequest()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound(ApiResponse.Fail(404, "User not found."));

        user.FirstName = dto.FirstName;
        user.LastName  = dto.LastName;
        await _userManager.UpdateAsync(user);

        return Ok(ApiResponse.NoContent("User name updated."));
    }

    private bool IsInternalRequest()
    {
        var key      = HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        var expected = _config["ServiceApiKey"];
        return !string.IsNullOrWhiteSpace(key) && key == expected;
    }
}

public sealed record UpdateUserNameDto(string FirstName, string LastName);
