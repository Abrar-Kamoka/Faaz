using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.DatabaseContext;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Booking.Infrastructure.Managers;

internal sealed class SessionManager : ISessionServices
{
    private readonly BookingDbContext _db;

    public SessionManager(BookingDbContext db)
    {
        _db = db;
    }

    public async Task<Session?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        return await _db.Sessions.FirstOrDefaultAsync(x => x.BookingId == bookingId, ct);
    }

    public async Task<Session?> GetByBookingIdWithParticipantsAsync(Guid bookingId, CancellationToken ct = default)
    {
        return await _db.Sessions
            .Include(x => x.Participants)
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.BookingId == bookingId, ct);
    }

    public async Task<Session?> GetByRoomNameAsync(string roomName, CancellationToken ct = default)
    {
        return await _db.Sessions.FirstOrDefaultAsync(x => x.LiveKitRoomName == roomName, ct);
    }

    public async Task AddAsync(Session session, CancellationToken ct = default)
    {
        await _db.Sessions.AddAsync(session, ct);
    }

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.Sessions.MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
