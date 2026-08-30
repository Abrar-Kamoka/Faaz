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
            .Where(a => !a.IsDeleted)
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

        // New announcements go live immediately (IsActive = true below) — an expiry that's already
        // passed would make it dead on arrival with no visible sign why, same problem as Activate.
        if (IsAlreadyExpired(req.ExpiresAt))
            return UnprocessableEntity(ApiResponse.Fail(422, "Expiry date must be in the future."));

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

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateAnnouncement(Guid id, CancellationToken ct)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var announcement = await _db.Announcements.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (announcement is null) return NotFound(ApiResponse.Fail(404, "Announcement not found."));

        // AnnouncementsController's public feed filters on IsActive AND (ExpiresAt == null OR
        // ExpiresAt > now) — flipping IsActive alone would silently do nothing if ExpiresAt is
        // still in the past. Rather than quietly rewriting the admin's own date for them (the
        // previous behavior here), refuse and say so — they decide what the new date should be.
        if (IsAlreadyExpired(announcement.ExpiresAt))
            return UnprocessableEntity(ApiResponse.Fail(422, "Can't activate — expiry date has already passed. Update it first."));

        // Re-publishing resets PublishedAt so it reads as "just went live" again rather than
        // showing its original (possibly long-past) first-publish date.
        announcement.IsActive    = true;
        announcement.PublishedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Announcement activated."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAnnouncement(Guid id, [FromBody] UpdateAnnouncementBody req, CancellationToken ct)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var announcement = await _db.Announcements.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (announcement is null) return NotFound(ApiResponse.Fail(404, "Announcement not found."));

        // Update never touches IsActive, so this only matters while the announcement is (still)
        // active — editing an inactive one is free to carry whatever past expiry it already had.
        if (announcement.IsActive && IsAlreadyExpired(req.ExpiresAt))
            return UnprocessableEntity(ApiResponse.Fail(422, "Can't save — expiry date must be in the future while this announcement is active."));

        announcement.Title     = req.Title;
        announcement.Body      = req.Body;
        announcement.Audience  = req.Audience;
        announcement.ExpiresAt = req.ExpiresAt;
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Announcement updated."));
    }

    private static bool IsAlreadyExpired(DateTime? expiresAt) => expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow;

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAnnouncement(Guid id, CancellationToken ct)
    {
        if (!IsInternal()) return StatusCode(403, ApiResponse.Fail(403, "Forbidden."));

        var announcement = await _db.Announcements.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (announcement is null) return NotFound(ApiResponse.Fail(404, "Announcement not found."));

        announcement.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return Ok(ApiResponse.NoContent("Announcement deleted."));
    }

    private bool IsInternal()
    {
        var key = HttpContext.Request.Headers["X-Service-Key"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(key) && key == _config["ServiceApiKey"];
    }
}

public record CreateAnnouncementBody(string Title, string Body, int Audience, DateTime? ExpiresAt, Guid AdminId);
public record UpdateAnnouncementBody(string Title, string Body, int Audience, DateTime? ExpiresAt);
