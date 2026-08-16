using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Faaz.Services.Notification.WebHost.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationLogServices _logServices;

    public NotificationsController(INotificationLogServices logServices)
    {
        _logServices = logServices;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetNotifications(
        Guid userId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken ct)
    {
        if (!IsOwnerOrAdmin(userId)) return StatusCode(403, ApiResponse.Fail(403, "Access denied."));
        var (items, total) = await _logServices.GetByUserIdAsync(userId, page, pageSize, ct);
        return Ok(ApiResponse.Ok(new { items, total }));
    }

    [HttpGet("{userId:guid}/unread-count")]
    public async Task<IActionResult> GetUnreadCount(Guid userId, CancellationToken ct)
    {
        if (!IsOwnerOrAdmin(userId)) return StatusCode(403, ApiResponse.Fail(403, "Access denied."));
        var count = await _logServices.GetUnreadCountAsync(userId, ct);
        return Ok(ApiResponse.Ok(new { count }));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, [FromQuery] Guid userId, CancellationToken ct)
    {
        if (!IsOwnerOrAdmin(userId)) return StatusCode(403, ApiResponse.Fail(403, "Access denied."));
        await _logServices.MarkAsReadAsync(id, userId, ct);
        await _logServices.SaveChangesAsync(ct);
        return Ok(ApiResponse.NoContent("Marked as read."));
    }

    [HttpPut("{userId:guid}/read-all")]
    public async Task<IActionResult> MarkAllAsRead(Guid userId, CancellationToken ct)
    {
        if (!IsOwnerOrAdmin(userId)) return StatusCode(403, ApiResponse.Fail(403, "Access denied."));
        await _logServices.MarkAllAsReadAsync(userId, ct);
        return Ok(ApiResponse.NoContent("All notifications marked as read."));
    }

    private bool IsOwnerOrAdmin(Guid userId)
    {
        var callerSub  = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var callerRole = User.FindFirstValue("role"); // "3" = Admin — see RoleClaimType config in Program.cs
        if (callerRole == "3") return true;
        return Guid.TryParse(callerSub, out var callerGuid) && callerGuid == userId;
    }
}
