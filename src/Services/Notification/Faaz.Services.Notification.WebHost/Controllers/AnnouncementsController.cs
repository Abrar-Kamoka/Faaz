using Faaz.Services.Notification.Infrastructure.DatabaseContext;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Notification.WebHost.Controllers;

[ApiController]
[Route("api/v1/announcements")]
[Authorize]
[Tags("Announcements")]
public class AnnouncementsController : ControllerBase
{
    private readonly NotificationDbContext _db;

    public AnnouncementsController(NotificationDbContext db) { _db = db; }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        // 0 = All audience — matches everyone regardless of their own role value.
        var role = int.TryParse(User.FindFirst("role")?.Value, out var r) ? r : 0;
        var now  = DateTime.UtcNow;

        var items = await _db.Announcements
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > now) && (a.Audience == 0 || a.Audience == role))
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => new { a.Id, a.Title, a.Body, a.PublishedAt })
            .ToListAsync(ct);

        return Ok(ApiResponse.Ok(items));
    }
}
