using Faaz.Services.Administration.Domain.Entities;

namespace Faaz.Services.Administration.Infrastructure.Interfaces;

public interface IReferenceDataRequestServices
{
    Task<(IReadOnlyList<ReferenceDataRequest> Items, int Total)> GetPagedAsync(
        ReferenceRequestStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<ReferenceDataRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ReferenceDataRequest request, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
