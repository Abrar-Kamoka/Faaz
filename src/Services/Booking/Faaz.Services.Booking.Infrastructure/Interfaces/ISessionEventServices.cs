using Faaz.Services.Booking.Domain.Entities;

namespace Faaz.Services.Booking.Infrastructure.Interfaces;

public interface ISessionEventServices
{
    Task<bool> ExistsByLiveKitEventIdAsync(string liveKitEventId, CancellationToken ct = default);
    Task AddAsync(SessionEvent evt, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
