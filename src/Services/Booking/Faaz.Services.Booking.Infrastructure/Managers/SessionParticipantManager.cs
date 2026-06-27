using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.DatabaseContext;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Booking.Infrastructure.Managers;

internal sealed class SessionParticipantManager : ISessionParticipantServices
{
    private readonly BookingDbContext _db;

    public SessionParticipantManager(BookingDbContext db)
    {
        _db = db;
    }

    public async Task<SessionParticipant?> GetBySessionAndUserAsync(Guid sessionId, Guid userId, CancellationToken ct = default)
    {
        return await _db.SessionParticipants
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.UserId == userId, ct);
    }

    public async Task<IReadOnlyList<SessionParticipant>> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _db.SessionParticipants
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(ct);
    }

    public async Task AddAsync(SessionParticipant participant, CancellationToken ct = default)
    {
        await _db.SessionParticipants.AddAsync(participant, ct);
    }

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.SessionParticipants.MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
