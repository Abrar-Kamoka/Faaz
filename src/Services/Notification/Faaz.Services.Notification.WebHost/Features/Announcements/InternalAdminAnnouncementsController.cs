using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.DatabaseContext;
using Faaz.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Notification.WebHost.Features.Announcements;

[Route("internal/admin/announcements")]
[Tags("Internal - Admin")]
[IgnoreAntiforgeryToken]
public class InternalAdminAnnouncementsController : ControllerBase
{
    private readonly NotificationDbContext _db;
    private readonly IConfiguration _config;

    public InternalAdminAnnouncementsController(NotificationDbContext db, IConfiguration config)
    {
        _db     = db;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> GetAnnouncements(CancellationToken ct)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var items = await _db.Announcements
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id, a.Title, a.Body, a.Audience, a.IsActive, a.PublishedAt, a.ExpiresAt, a.CreatedByAdminId
            })
            .ToListAsync(ct);

        return Ok(ApiResponse.Ok(items));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementBody req, CancellationToken ct)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var srNo = await _db.Announcements.MaxAsync(a => (int?)a.SrNo, ct) ?? 0;
        var announcement = new Announcement
        {
            SrNo             = srNo + 1,
            Title            = req.Title,
            Body             = req.Body,
            Audience         = req.Audience,
            IsActive         = true,
            PublishedAt      = DateTime.UtcNow,
            ExpiresAt        = req.ExpiresAt,
            CreatedByAdminId = req.AdminId
        };
        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync(ct);

        return StatusCode(201, ApiResponse.Created(new { announcement.Id }));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateAnnouncement(Guid id, CancellationToken ct)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var announcement = await _db.Announcements.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (announcement is null) return NotFound(ApiResponse.Fail(404, "Announcement not found."));

        announcement.IsActive = false;
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Announcement deactivated."));
    }

    private bool IsInternal()
    {
        var key = HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(key) && key == _config["ServiceApiKey"];
    }
}

public record CreateAnnouncementBody(string Title, string Body, int Audience, DateTime? ExpiresAt, Guid AdminId);
