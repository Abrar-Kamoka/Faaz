using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.DatabaseContext;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.Infrastructure.Managers;

internal sealed class NotificationLogManager : INotificationLogServices
{
    private readonly NotificationDbContext _db;

    public NotificationLogManager(NotificationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(NotificationLog log, CancellationToken ct = default)
    {
        await _db.NotificationLogs.AddAsync(log, ct);
    }

    public async Task<(IReadOnlyList<NotificationLog> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        // Channel == InApp only: Email-channel rows are an audit trail of the actual email that was
        // sent, so their Body is raw HTML (<p>, <a href>...) meant for a mail client — surfacing them
        // here renders as literal escaped tags in the bell/drawer instead of the email UI they were
        // written for. SentAt over CreatedAt — every consumer sets SentAt explicitly and reliably,
        // whereas CreatedAt relies on the auto-stamp interceptor and was NULL on every row for a while
        // (see AddNotificationInfrastructure), which would otherwise sort those rows arbitrarily.
        var query = _db.NotificationLogs
            .Where(x => x.UserId == userId && !x.IsDeleted && x.Channel == NotificationChannel.InApp)
            .OrderByDescending(x => x.SentAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.NotificationLogs.CountAsync(
            x => x.UserId == userId && !x.IsRead && !x.IsDeleted && x.Channel == NotificationChannel.InApp, ct);
    }

    public async Task MarkAsReadAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var log = await _db.NotificationLogs.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (log is null) return;
        log.IsRead = true;
        log.ReadAt = DateTime.UtcNow;
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        await _db.NotificationLogs
            .Where(x => x.UserId == userId && !x.IsRead && x.Channel == NotificationChannel.InApp)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsRead, true)
                .SetProperty(x => x.ReadAt, DateTime.UtcNow), ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
