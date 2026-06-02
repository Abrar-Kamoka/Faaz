using Faaz.Services.Identity.Domain.Entities;

namespace Faaz.Services.Identity.Infrastructure.Interfaces.Auth;

public interface IRefreshTokenServices
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);
    Task<RefreshToken?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task AddAsync(RefreshToken token, CancellationToken ct);
    Task RevokeAllForUserAsync(Guid userId, string? revokedByIp, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
