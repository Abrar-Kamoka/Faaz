using Faaz.Services.Student.Domain.Entities;
using Faaz.Services.Student.Infrastructure.DatabaseContext;
using Faaz.Services.Student.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Faaz.Services.Student.Domain.StudentEnums;

namespace Faaz.Services.Student.Infrastructure.Managers;

internal sealed class StudentProfileManager : IStudentProfileServices
{
    private readonly StudentDbContext _db;

    public StudentProfileManager(StudentDbContext db)
    {
        _db = db;
    }

    public Task<StudentProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return _db.StudentProfiles
            .Include(p => p.SixthFormData)
            .Include(p => p.UndergraduateData)
            .Include(p => p.PostgraduateData)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    public Task<StudentProfile?> GetByUserIdNoTrackingAsync(Guid userId, CancellationToken ct)
    {
        return _db.StudentProfiles
            .AsNoTracking()
            .Include(p => p.SixthFormData)
            .Include(p => p.UndergraduateData)
            .Include(p => p.PostgraduateData)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    public Task<bool> ExistsForUserAsync(Guid userId, CancellationToken ct)
    {
        return _db.StudentProfiles.AnyAsync(p => p.UserId == userId, ct);
    }

    public async Task AddAsync(StudentProfile profile, CancellationToken ct)
    {
        await _db.StudentProfiles.AddAsync(profile, ct);
    }

    public void Remove<T>(T entity) where T : class
    {
        _db.Set<T>().Remove(entity);
    }

    public async Task AddStudyDataAsync<T>(T entity, CancellationToken ct) where T : class
    {
        await _db.Set<T>().AddAsync(entity, ct);
    }

    public void DetachEntity<T>(T entity) where T : class
    {
        _db.Entry(entity).State = EntityState.Detached;
    }

    public Task ExecuteUpdateStudyTrackAsync(Guid profileId, StudyTrack track, int completeness, bool isOnboarding, CancellationToken ct)
    {
        return _db.StudentProfiles
            .Where(p => p.Id == profileId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.StudyTrack, track)
                .SetProperty(p => p.ProfileCompleteness, completeness)
                .SetProperty(p => p.IsOnboardingComplete, isOnboarding)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
            ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
    }
}
