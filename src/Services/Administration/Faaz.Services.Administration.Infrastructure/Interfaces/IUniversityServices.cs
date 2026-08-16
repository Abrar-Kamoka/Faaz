using Faaz.Services.Administration.Domain.Entities;

namespace Faaz.Services.Administration.Infrastructure.Interfaces;

public interface IUniversityServices
{
    Task<(IReadOnlyList<University> Items, int Total)> GetPagedAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task<University?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(University university, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
