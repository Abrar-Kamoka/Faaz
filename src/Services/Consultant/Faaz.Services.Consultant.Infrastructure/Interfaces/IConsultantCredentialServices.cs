using Faaz.Services.Consultant.Domain.Entities;

namespace Faaz.Services.Consultant.Infrastructure.Interfaces;

public interface IConsultantCredentialServices
{
    Task<ConsultantCredential?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ConsultantCredential>> GetByProfileIdAsync(Guid profileId, CancellationToken ct = default);
    Task AddAsync(ConsultantCredential credential, CancellationToken ct = default);
    void Delete(ConsultantCredential credential);
    Task SaveChangesAsync(CancellationToken ct = default);
}
