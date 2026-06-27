using Faaz.Services.Booking.Domain.Entities;

namespace Faaz.Services.Booking.Infrastructure.Interfaces;

public interface IRefundAppealServices
{
    Task<RefundAppeal?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<RefundAppeal?> GetByIdAsync(Guid appealId, CancellationToken ct = default);
    Task<(IReadOnlyList<RefundAppeal> Items, int TotalCount)> GetPendingAsync(int page, int pageSize, CancellationToken ct = default);
    Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken ct = default);
    Task AddAsync(RefundAppeal appeal, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
