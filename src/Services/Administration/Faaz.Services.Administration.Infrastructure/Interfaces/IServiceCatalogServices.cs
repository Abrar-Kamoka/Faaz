using Faaz.Services.Administration.Domain.Entities;

namespace Faaz.Services.Administration.Infrastructure.Interfaces;

// Named ServiceCatalog (not "ServiceServices") to avoid the awkward stutter against the Service entity.
public interface IServiceCatalogServices
{
    Task<(IReadOnlyList<Service> Items, int Total)> GetPagedAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task<Service?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Service service, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
