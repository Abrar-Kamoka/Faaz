using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Administration.Infrastructure.Managers;

internal sealed class AdminActionLogManager : IAdminActionLogServices
{
    private readonly AdminDbContext _db;
    public AdminActionLogManager(AdminDbContext db) { _db = db; }

    public async Task AddAsync(AdminActionLog log, CancellationToken ct = default)
        => await _db.AdminActionLogs.AddAsync(log, ct);

    public async Task<(IReadOnlyList<AdminActionLog> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, Guid? adminId = null, string? entityType = null, CancellationToken ct = default)
    {
        var query = _db.AdminActionLogs.AsQueryable();
        if (adminId.HasValue)
            query = query.Where(x => x.AdminUserId == adminId.Value);
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(x => x.EntityType == entityType);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.AdminActionLogs.MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
