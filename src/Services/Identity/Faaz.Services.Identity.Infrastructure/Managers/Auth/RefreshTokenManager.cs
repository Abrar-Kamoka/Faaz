using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.DatabaseContext;
using Faaz.Services.Identity.Infrastructure.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Identity.Infrastructure.Managers.Auth;

internal sealed class RefreshTokenManager : IRefreshTokenServices
{
    private readonly IdentityDbContext _db;

    public RefreshTokenManager(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct)
    {
        return await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenHash, ct);
    }

    public async Task<RefreshToken?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await _db.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == userId, ct);
    }

    public async Task AddAsync(RefreshToken token, CancellationToken ct)
    {
        await _db.RefreshTokens.AddAsync(token, ct);
    }

    public async Task RevokeAllForUserAsync(Guid userId, string? revokedByIp, CancellationToken ct)
    {
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedByIp = revokedByIp;
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);
    }

}
