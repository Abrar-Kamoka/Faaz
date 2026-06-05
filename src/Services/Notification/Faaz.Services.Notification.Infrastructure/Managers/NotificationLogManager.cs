using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.DatabaseContext;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

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
        var query = _db.NotificationLogs
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.NotificationLogs.CountAsync(x => x.UserId == userId && !x.IsRead && !x.IsDeleted, ct);
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
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsRead, true)
                .SetProperty(x => x.ReadAt, DateTime.UtcNow), ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
