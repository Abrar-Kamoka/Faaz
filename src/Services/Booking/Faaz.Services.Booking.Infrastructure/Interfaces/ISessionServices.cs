using Faaz.Services.Booking.Domain.Entities;

namespace Faaz.Services.Booking.Infrastructure.Interfaces;

public interface ISessionServices
{
    Task<Session?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<Session?> GetByBookingIdWithParticipantsAsync(Guid bookingId, CancellationToken ct = default);
    Task<Session?> GetByRoomNameAsync(string roomName, CancellationToken ct = default);
    Task AddAsync(Session session, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
