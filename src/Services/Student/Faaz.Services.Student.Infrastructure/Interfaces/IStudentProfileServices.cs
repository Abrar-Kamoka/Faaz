using Faaz.Services.Student.Domain.Entities;
using static Faaz.Services.Student.Domain.StudentEnums;

namespace Faaz.Services.Student.Infrastructure.Interfaces;

public interface IStudentProfileServices
{
    Task<StudentProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<StudentProfile?> GetByUserIdNoTrackingAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsForUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(StudentProfile profile, CancellationToken ct = default);
    Task AddStudyDataAsync<T>(T entity, CancellationToken ct = default) where T : class;
    void Remove<T>(T entity) where T : class;
    void DetachEntity<T>(T entity) where T : class;
    Task ExecuteUpdateStudyTrackAsync(Guid profileId, StudyTrack track, int completeness, bool isOnboarding, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
