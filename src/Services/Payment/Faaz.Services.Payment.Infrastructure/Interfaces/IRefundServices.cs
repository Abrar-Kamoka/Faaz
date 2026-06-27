using Faaz.Services.Payment.Domain.Entities;

namespace Faaz.Services.Payment.Infrastructure.Interfaces;

public interface IRefundServices
{
    Task<Refund?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Refund>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task AddAsync(Refund refund, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
