using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Administration.Infrastructure.Managers;

internal sealed class ReferenceDataRequestManager : IReferenceDataRequestServices
{
    private readonly AdminDbContext _db;
    public ReferenceDataRequestManager(AdminDbContext db) { _db = db; }

    public async Task<(IReadOnlyList<ReferenceDataRequest> Items, int Total)> GetPagedAsync(
        ReferenceRequestStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.ReferenceDataRequests.AsQueryable();
        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreatedAt)
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return (items, total);
    }

    public async Task<ReferenceDataRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.ReferenceDataRequests.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(ReferenceDataRequest request, CancellationToken ct = default)
        => await _db.ReferenceDataRequests.AddAsync(request, ct);

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.ReferenceDataRequests.IgnoreQueryFilters()
                           .MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
