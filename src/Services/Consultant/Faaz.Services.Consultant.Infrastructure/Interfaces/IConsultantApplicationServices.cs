using Faaz.Services.Consultant.Domain.Entities;
using static Faaz.Services.Consultant.Domain.ConsultantEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.Infrastructure.Interfaces;

public interface IConsultantApplicationServices
{
    Task<ConsultantApplication?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ConsultantApplication?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<ConsultantApplication?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default);
    Task<ConsultantApplication?> GetByInviteTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<(IReadOnlyList<ConsultantApplication> Items, int TotalCount)> GetPagedAsync(ConsultantApplicationStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<ConsultantApplication?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(ConsultantApplication application, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
