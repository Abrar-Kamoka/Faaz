using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Administration.Infrastructure.Managers;

internal sealed class ServiceCatalogManager : IServiceCatalogServices
{
    private readonly AdminDbContext _db;
    public ServiceCatalogManager(AdminDbContext db) { _db = db; }

    public async Task<(IReadOnlyList<Service> Items, int Total)> GetPagedAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Services.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Name.Contains(search) || (x.Category != null && x.Category.Contains(search)));
        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);
        var total = await q.CountAsync(ct);

        var ordered = string.IsNullOrWhiteSpace(search)
            ? q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            : q.OrderByDescending(x => x.Name.StartsWith(search)).ThenBy(x => x.SortOrder).ThenBy(x => x.Name);

        var items = await ordered
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return (items, total);
    }

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Services.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(Service service, CancellationToken ct = default)
        => await _db.Services.AddAsync(service, ct);

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.Services.IgnoreQueryFilters()
                           .MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
