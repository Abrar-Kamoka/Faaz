using Microsoft.AspNetCore.Mvc;

namespace Faaz.Services.Payment.WebHost;

[ApiController]
public abstract class FaazApiController : ControllerBase
{
    protected Guid GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    protected string GetRole() => User.FindFirst("role")?.Value ?? "";

    protected bool IsOwner(Guid resourceOwnerId) => GetUserId() == resourceOwnerId;

    protected bool IsOwnerOrAdmin(Guid resourceOwnerId)
    {
        var role = GetRole();
        return role == "3" || GetUserId() == resourceOwnerId;
    }
}
