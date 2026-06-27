using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Faaz.Services.Booking.WebHost;

[ApiController]
[Produces("application/json")]
[ProducesResponseType(typeof(object), 400)]
[ProducesResponseType(typeof(object), 401)]
[ProducesResponseType(typeof(object), 403)]
[ProducesResponseType(typeof(object), 500)]
public abstract class FaazApiController : ControllerBase
{
    protected Guid GetUserId()    => Guid.Parse(User.FindFirstValue("sub")!);
    protected string GetRole()    => User.FindFirstValue("role") ?? "0";

    protected bool IsOwner(Guid resourceUserId)
        => GetUserId() == resourceUserId;

    protected bool IsOwnerOrAdmin(Guid resourceUserId)
    {
        var role = GetRole();
        return role == "3" || GetUserId() == resourceUserId;
    }
}
