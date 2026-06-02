using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.DatabaseContext;
using Faaz.Services.Identity.Infrastructure.Interfaces.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Identity.Infrastructure.Managers.Users;

internal sealed class ApplicationUserRepository : IApplicationUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityDbContext _db;

    public ApplicationUserRepository(UserManager<ApplicationUser> userManager, IdentityDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _userManager.FindByEmailAsync(email);

    public async Task<ApplicationUser?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct = default)
        => await _db.Users.FirstOrDefaultAsync(
            u => u.PhoneNumber == phoneNumber && !u.IsDeleted, ct);
}
