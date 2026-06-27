using Faaz.Services.Booking.Domain.Entities;

namespace Faaz.Services.Booking.Infrastructure.Interfaces;

public interface ISessionParticipantServices
{
    Task<SessionParticipant?> GetBySessionAndUserAsync(Guid sessionId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<SessionParticipant>> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default);
    Task AddAsync(SessionParticipant participant, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
