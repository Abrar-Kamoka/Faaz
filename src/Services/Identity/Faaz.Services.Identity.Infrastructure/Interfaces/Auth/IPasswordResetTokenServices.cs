using Faaz.Services.Identity.Domain.Entities;

namespace Faaz.Services.Identity.Infrastructure.Interfaces.Auth;

public interface IPasswordResetTokenServices
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);
    Task<PasswordResetToken?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct);
    Task AddAsync(PasswordResetToken token, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
