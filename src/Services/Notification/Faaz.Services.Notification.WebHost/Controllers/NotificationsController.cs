using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        var (items, total) = await _logServices.GetByUserIdAsync(userId, page, pageSize, ct);
        return Ok(ApiResponse.Ok(new { items, total }));
    }

    [HttpGet("{userId:guid}/unread-count")]
    public async Task<IActionResult> GetUnreadCount(Guid userId, CancellationToken ct)
    {
        var count = await _logServices.GetUnreadCountAsync(userId, ct);
        return Ok(ApiResponse.Ok(new { count }));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, [FromQuery] Guid userId, CancellationToken ct)
    {
        await _logServices.MarkAsReadAsync(id, userId, ct);
        await _logServices.SaveChangesAsync(ct);
        return Ok(ApiResponse.NoContent("Marked as read."));
    }

    [HttpPut("{userId:guid}/read-all")]
    public async Task<IActionResult> MarkAllAsRead(Guid userId, CancellationToken ct)
    {
        await _logServices.MarkAllAsReadAsync(userId, ct);
        return Ok(ApiResponse.NoContent("All notifications marked as read."));
    }
}
