using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.DatabaseContext;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Payment.Infrastructure.Managers;

internal sealed class PromoCodeManager : IPromoCodeServices
{
    private readonly PaymentDbContext _db;

    public PromoCodeManager(PaymentDbContext db) { _db = db; }

    public async Task<PromoCode?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await _db.PromoCodes.FirstOrDefaultAsync(x => x.Code == code && x.IsActive, ct);

    public async Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.PromoCodes.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(PromoCode promoCode, CancellationToken ct = default)
        => await _db.PromoCodes.AddAsync(promoCode, ct);

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.PromoCodes.MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
