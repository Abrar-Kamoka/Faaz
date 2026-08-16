using Faaz.Services.Student.Domain.Entities;
using Faaz.Services.Student.Infrastructure.DatabaseContext;
using Faaz.Services.Student.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Student.Infrastructure.Managers;

internal sealed class SavedConsultantManager : ISavedConsultantServices
{
    private readonly StudentDbContext _db;

    public SavedConsultantManager(StudentDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SavedConsultant>> GetByStudentIdAsync(Guid studentUserId, CancellationToken ct = default)
    {
        return await _db.SavedConsultants
            .Where(s => s.StudentUserId == studentUserId)
            .OrderByDescending(s => s.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<bool> ExistsAsync(Guid studentUserId, Guid consultantUserId, CancellationToken ct = default)
    {
        return _db.SavedConsultants.AnyAsync(
            s => s.StudentUserId == studentUserId && s.ConsultantUserId == consultantUserId, ct);
    }

    public async Task AddAsync(SavedConsultant entity, CancellationToken ct = default)
    {
        await _db.SavedConsultants.AddAsync(entity, ct);
    }

    public async Task<bool> RemoveAsync(Guid studentUserId, Guid consultantUserId, CancellationToken ct = default)
    {
        var entity = await _db.SavedConsultants.FirstOrDefaultAsync(
            s => s.StudentUserId == studentUserId && s.ConsultantUserId == consultantUserId, ct);
        if (entity is null) return false;

        _db.SavedConsultants.Remove(entity);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }
}
