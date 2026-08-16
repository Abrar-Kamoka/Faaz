using Faaz.Services.Administration.Domain.Entities;

namespace Faaz.Services.Administration.Infrastructure.Interfaces;

public interface IDisputeNoteServices
{
    Task<IReadOnlyList<DisputeNote>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task AddAsync(DisputeNote note, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
