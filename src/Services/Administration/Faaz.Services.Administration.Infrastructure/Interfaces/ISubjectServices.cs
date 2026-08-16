using Faaz.Services.Administration.Domain.Entities;

namespace Faaz.Services.Administration.Infrastructure.Interfaces;

public interface ISubjectServices
{
    Task<(IReadOnlyList<Subject> Items, int Total)> GetPagedAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task<Subject?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Subject subject, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
