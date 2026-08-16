using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Auth;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Faaz.Services.Identity.WebHost.Features.Gdpr;

[Route("api/v1/auth/me/gdpr")]
[Tags("GDPR")]
[Authorize]
public class GdprController : FaazApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenServices _refreshTokens;

    public GdprController(UserManager<ApplicationUser> userManager, IRefreshTokenServices refreshTokens)
    {
        _userManager   = userManager;
        _refreshTokens = refreshTokens;
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var user   = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound(ApiResponse.Fail(404, "User not found."));

        var export = new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            Role                         = user.Role.ToString(),
            Status                       = user.Status.ToString(),
            user.IsEmailVerified,
            ConsultantApplicationStatus  = user.ConsultantApplicationStatus?.ToString(),
            user.CreatedAt,
            user.LastLoginAt
        };

        return Ok(ApiResponse.Ok(export));
    }

    [HttpDelete("erase")]
    public async Task<IActionResult> Erase(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var user   = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound(ApiResponse.Fail(404, "User not found."));

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _refreshTokens.RevokeAllForUserAsync(userId, ip, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        var anon = $"deleted-{userId.ToString()[..8]}@erased.faaz";
        user.Email            = anon;
        user.NormalizedEmail  = anon.ToUpperInvariant();
        user.UserName         = anon;
        user.NormalizedUserName = anon.ToUpperInvariant();
        user.FirstName        = "Deleted";
        user.LastName         = "User";
        user.PhoneNumber      = null;
        user.IsDeleted        = true;
        user.DeletedAt        = DateTime.UtcNow;
        user.SecurityStamp    = Guid.NewGuid().ToString();

        await _userManager.UpdateAsync(user);

        Response.Cookies.Delete("faaz_rt");

        return Ok(ApiResponse.NoContent("Account erased and all sessions revoked."));
    }
}
