using Faaz.Services.Student.Domain.Entities;

namespace Faaz.Services.Student.Infrastructure.Interfaces;

public interface ISavedConsultantServices
{
    Task<IReadOnlyList<SavedConsultant>> GetByStudentIdAsync(Guid studentUserId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid studentUserId, Guid consultantUserId, CancellationToken ct = default);
    Task AddAsync(SavedConsultant entity, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid studentUserId, Guid consultantUserId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
