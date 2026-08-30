using Faaz.Services.Administration.Domain.Entities;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Administration.Infrastructure.Interfaces;

public interface IProgrammeServices
{
    Task<(IReadOnlyList<Programme> Items, int Total)> GetPagedAsync(
        Guid? universityId, StudyLevel? studyLevel, Guid? subjectId, string? search, bool? isActive,
        int page, int pageSize, CancellationToken ct = default);
    Task<Programme?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Programme programme, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
