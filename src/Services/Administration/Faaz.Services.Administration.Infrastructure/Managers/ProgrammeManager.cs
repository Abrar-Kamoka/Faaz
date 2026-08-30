using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.DatabaseContext;
using Faaz.Services.Administration.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Administration.Infrastructure.Managers;

internal sealed class ProgrammeManager : IProgrammeServices
{
    private readonly AdminDbContext _db;
    public ProgrammeManager(AdminDbContext db) { _db = db; }

    public async Task<(IReadOnlyList<Programme> Items, int Total)> GetPagedAsync(
        Guid? universityId, StudyLevel? studyLevel, Guid? subjectId, string? search, bool? isActive,
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Programmes.AsQueryable();

        if (universityId.HasValue)
            q = q.Where(x => x.UniversityId == universityId.Value);
        if (studyLevel.HasValue)
            q = q.Where(x => x.StudyLevel == studyLevel.Value);
        if (subjectId.HasValue)
            q = q.Where(x => x.ProgrammeSubjects.Any(ps => ps.SubjectId == subjectId.Value));
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Title.Contains(search) || (x.UcasCode != null && x.UcasCode.Contains(search)));
        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        var total = await q.CountAsync(ct);

        var ordered = string.IsNullOrWhiteSpace(search)
            ? q.OrderBy(x => x.Title)
            : q.OrderByDescending(x => x.Title.StartsWith(search)).ThenBy(x => x.Title);

        var items = await ordered
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return (items, total);
    }

    public async Task<Programme?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Programmes.Include(x => x.ProgrammeSubjects)
                                .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(Programme programme, CancellationToken ct = default)
        => await _db.Programmes.AddAsync(programme, ct);

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.Programmes.IgnoreQueryFilters()
                           .MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
