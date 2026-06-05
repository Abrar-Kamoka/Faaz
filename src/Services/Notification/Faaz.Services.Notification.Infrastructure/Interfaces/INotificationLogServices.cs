using Faaz.Services.Notification.Domain.Entities;

namespace Faaz.Services.Notification.Infrastructure.Interfaces;

public interface INotificationLogServices
{
    Task AddAsync(NotificationLog log, CancellationToken ct = default);
    Task<(IReadOnlyList<NotificationLog> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
