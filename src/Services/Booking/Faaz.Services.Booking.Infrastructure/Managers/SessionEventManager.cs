using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.DatabaseContext;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Booking.Infrastructure.Managers;

internal sealed class SessionEventManager : ISessionEventServices
{
    private readonly BookingDbContext _db;

    public SessionEventManager(BookingDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExistsByLiveKitEventIdAsync(string liveKitEventId, CancellationToken ct = default)
    {
        // SessionEvent extends BaseEntity (no IsDeleted filter), so no IgnoreQueryFilters needed
        return await _db.SessionEvents.AnyAsync(x => x.LiveKitEventId == liveKitEventId, ct);
    }

    public async Task AddAsync(SessionEvent evt, CancellationToken ct = default)
    {
        await _db.SessionEvents.AddAsync(evt, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
