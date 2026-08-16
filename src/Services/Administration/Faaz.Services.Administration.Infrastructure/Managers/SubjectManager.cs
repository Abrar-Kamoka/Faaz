using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Administration.Infrastructure.Managers;

internal sealed class SubjectManager : ISubjectServices
{
    private readonly AdminDbContext _db;
    public SubjectManager(AdminDbContext db) { _db = db; }

    public async Task<(IReadOnlyList<Subject> Items, int Total)> GetPagedAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Subjects.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Name.Contains(search) || (x.Category != null && x.Category.Contains(search)));
        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(x => x.Name)
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return (items, total);
    }

    public async Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Subjects.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(Subject subject, CancellationToken ct = default)
        => await _db.Subjects.AddAsync(subject, ct);

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.Subjects.IgnoreQueryFilters()
                           .MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
