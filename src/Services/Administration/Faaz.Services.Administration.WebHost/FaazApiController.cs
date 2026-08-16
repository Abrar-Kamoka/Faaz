using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Faaz.Services.Administration.WebHost;

[ApiController]
[Produces("application/json")]
[ProducesResponseType(typeof(object), 400)]
[ProducesResponseType(typeof(object), 401)]
[ProducesResponseType(typeof(object), 403)]
[ProducesResponseType(typeof(object), 500)]
public abstract class FaazApiController : ControllerBase
{
    protected Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub")!);
    protected string GetRole() => User.FindFirstValue("role") ?? "0";

    // Additive, fine-grained gate on top of [Authorize(Policy = "AdminOnly")] — every admin JWT that
    // predates Roles Management (or belongs to the built-in Admin role) carries every permission
    // claim, so this is a no-op for them. Only a staff member moved onto a narrower custom role
    // (see Roles Management) will ever actually be denied here.
    protected bool HasPermission(string permissionKey) =>
        User.HasClaim("permission", permissionKey);

    protected IActionResult? RequirePermission(string permissionKey) =>
        HasPermission(permissionKey)
            ? null
            : StatusCode(403, Faaz.SharedKernel.Results.ApiResponse.Fail(403, $"Missing required permission: {permissionKey}"));
}
