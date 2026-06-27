using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.DatabaseContext;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Faaz.Services.Booking.Domain.BookingEnums;

namespace Faaz.Services.Booking.Infrastructure.Managers;

internal sealed class RefundAppealManager : IRefundAppealServices
{
    private readonly BookingDbContext _db;

    public RefundAppealManager(BookingDbContext db)
    {
        _db = db;
    }

    public async Task<RefundAppeal?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        return await _db.RefundAppeals.FirstOrDefaultAsync(x => x.BookingId == bookingId, ct);
    }

    public async Task<RefundAppeal?> GetByIdAsync(Guid appealId, CancellationToken ct = default)
    {
        return await _db.RefundAppeals.FindAsync([appealId], ct);
    }

    public async Task<(IReadOnlyList<RefundAppeal> Items, int TotalCount)> GetPendingAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.RefundAppeals
            .Include(x => x.Booking)
            .Where(x => x.Status == RefundAppealStatus.Pending)
            .OrderBy(x => x.SubmittedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken ct = default)
    {
        return await _db.RefundAppeals.AnyAsync(x => x.BookingId == bookingId, ct);
    }

    public async Task AddAsync(RefundAppeal appeal, CancellationToken ct = default)
    {
        await _db.RefundAppeals.AddAsync(appeal, ct);
    }

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.RefundAppeals.MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}

