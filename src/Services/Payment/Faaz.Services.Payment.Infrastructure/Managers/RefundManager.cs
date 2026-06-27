using Faaz.Services.Payment.Domain.Entities;
using Faaz.Services.Payment.Infrastructure.DatabaseContext;
using Faaz.Services.Payment.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Payment.Infrastructure.Managers;

internal sealed class RefundManager : IRefundServices
{
    private readonly PaymentDbContext _db;

    public RefundManager(PaymentDbContext db) { _db = db; }

    public async Task<Refund?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Refunds.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Refund>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
        => await _db.Refunds.Where(x => x.BookingId == bookingId).ToListAsync(ct);

    public async Task AddAsync(Refund refund, CancellationToken ct = default)
        => await _db.Refunds.AddAsync(refund, ct);

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.Refunds.MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
