using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.DatabaseContext;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Payment.Infrastructure.Managers;

internal sealed class PayoutManager : IPayoutServices
{
    private readonly PaymentDbContext _db;

    public PayoutManager(PaymentDbContext db) { _db = db; }

    public async Task<Payout?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Payouts.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Payout?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
        => await _db.Payouts.FirstOrDefaultAsync(x => x.BookingId == bookingId, ct);

    public async Task<(IReadOnlyList<Payout> Items, int TotalCount)> GetByConsultantAsync(
        Guid consultantUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Payouts
            .Where(x => x.ConsultantUserId == consultantUserId)
            .OrderByDescending(x => x.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(Payout payout, CancellationToken ct = default)
        => await _db.Payouts.AddAsync(payout, ct);

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.Payouts.MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
