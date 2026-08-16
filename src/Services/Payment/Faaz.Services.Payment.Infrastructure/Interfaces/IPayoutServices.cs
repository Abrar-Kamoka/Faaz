using Faaz.Services.Payment.Domain.Entities;

namespace Faaz.Services.Payment.Infrastructure.Interfaces;

public interface IPayoutServices
{
    Task<Payout?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Payout?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<(IReadOnlyList<Payout> Items, int TotalCount)> GetByConsultantAsync(Guid consultantUserId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Payout payout, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    Task<(IReadOnlyList<Payout> Items, int TotalCount)> GetAllForAdminAsync(
        int page, int pageSize, string? status, CancellationToken ct = default);
}
