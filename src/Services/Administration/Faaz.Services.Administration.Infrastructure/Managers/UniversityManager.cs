using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Administration.Infrastructure.Managers;

internal sealed class UniversityManager : IUniversityServices
{
    private readonly AdminDbContext _db;
    public UniversityManager(AdminDbContext db) { _db = db; }

    public async Task<(IReadOnlyList<University> Items, int Total)> GetPagedAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Universities.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Name.Contains(search) || (x.Country != null && x.Country.Contains(search)));
        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);
        var total = await q.CountAsync(ct);

        // StartsWith matches first, then Contains matches, both then alphabetical — plain
        // Contains+OrderBy(Name) buries the obvious match once the list runs into the hundreds.
        var ordered = string.IsNullOrWhiteSpace(search)
            ? q.OrderBy(x => x.Name)
            : q.OrderByDescending(x => x.Name.StartsWith(search)).ThenBy(x => x.Name);

        var items = await ordered
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return (items, total);
    }

    public async Task<University?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Universities.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(University university, CancellationToken ct = default)
        => await _db.Universities.AddAsync(university, ct);

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.Universities.IgnoreQueryFilters()
                           .MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
