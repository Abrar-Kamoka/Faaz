using Faaz.Services.Payment.Domain.Entities;

namespace Faaz.Services.Payment.Infrastructure.Interfaces;

public interface IPromoCodeServices
{
    Task<PromoCode?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(PromoCode promoCode, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
