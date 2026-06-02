using Faaz.Services.Identity.Domain.Entities;

namespace Faaz.Services.Identity.Infrastructure.Interfaces.Users;

public interface IApplicationUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<ApplicationUser?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default);
}
