using Faaz.Services.Administration.Domain.Entities;

namespace Faaz.Services.Administration.Infrastructure.Interfaces;

public interface IPlatformConfigServices
{
    Task<PlatformConfig?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<PlatformConfig>> GetAllAsync(CancellationToken ct = default);
    Task UpsertAsync(string key, string value, string? description, Guid adminId, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
