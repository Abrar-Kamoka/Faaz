using Faaz.Services.Payment.Domain.Entities;

namespace Faaz.Services.Payment.Infrastructure.Interfaces;

public interface IPromoCodeServices
{
    Task<PromoCode?> GetByCodeAsync(string code, CancellationToken ct = default);
    // Unlike GetByCodeAsync, checks ALL codes (active or deactivated) — Code has a DB-level unique
    // index regardless of IsActive, so a deactivated code still blocks reuse and must be caught here.
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);
    Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<PromoCode> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(PromoCode promoCode, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
