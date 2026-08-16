using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Administration.Infrastructure.Managers;

internal sealed class DisputeNoteManager : IDisputeNoteServices
{
    private readonly AdminDbContext _db;
    public DisputeNoteManager(AdminDbContext db) { _db = db; }

    public async Task<IReadOnlyList<DisputeNote>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
        => await _db.DisputeNotes
                    .Where(x => x.BookingId == bookingId)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync(ct);

    public async Task AddAsync(DisputeNote note, CancellationToken ct = default)
        => await _db.DisputeNotes.AddAsync(note, ct);

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.DisputeNotes.MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
