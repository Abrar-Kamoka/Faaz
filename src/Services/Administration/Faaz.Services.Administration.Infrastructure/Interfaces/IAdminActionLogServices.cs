using Faaz.Services.Administration.Domain.Entities;

namespace Faaz.Services.Administration.Infrastructure.Interfaces;

public interface IAdminActionLogServices
{
    Task AddAsync(AdminActionLog log, CancellationToken ct = default);
    Task<(IReadOnlyList<AdminActionLog> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize,
        Guid? adminId = null,
        string? entityType = null,
        CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
