using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.DatabaseContext;
using Faaz.Services.Identity.Infrastructure.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Identity.Infrastructure.Managers.Auth;

internal sealed class PasswordResetTokenManager : IPasswordResetTokenServices
{
    private readonly IdentityDbContext _db;

    public PasswordResetTokenManager(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct)
    {
        var obj = await _db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == tokenHash && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow, ct);
        return obj;
    }

    public async Task<PasswordResetToken?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var obj = await _db.PasswordResetTokens
            .Where(t => t.UserId == userId && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
        return obj;
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct)
    {
        await _db.PasswordResetTokens.AddAsync(token, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);
    }

}
