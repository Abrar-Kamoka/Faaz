using Faaz.Services.Identity.Domain;
using Faaz.Services.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Faaz.Services.Identity.Domain.IdentityEnums;

namespace Faaz.Services.Identity.WebHost.Seeding;

// Idempotent, runtime-checked seed for the 3 built-in system roles — replaces the old migration-time
// HasData() seed. HasData only inserts the first time its migration applies; if the row is ever
// deleted afterwards (e.g. a full data reset), a plain `Database.Migrate()` never re-creates it, since
// EF sees that migration as already applied. Checking "does it exist? if not, insert" on every boot,
// like the other seeders in this folder, makes the built-in roles self-healing after any reset.
public static class RoleSeeder
{
    private static readonly (Guid Id, UserRole RoleType)[] SystemRoles =
    [
        (SystemRoleIds.SuperAdmin, UserRole.SuperAdmin),
        (SystemRoleIds.Student,    UserRole.Student),
        (SystemRoleIds.Consultant, UserRole.Consultant),
    ];

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var (id, roleType) in SystemRoles)
        {
            if (await roleManager.FindByIdAsync(id.ToString()) is not null) continue;

            var name = roleType.ToString();
            var role = new ApplicationRole(name)
            {
                Id               = id,
                NormalizedName   = name.ToUpperInvariant(),
                ConcurrencyStamp = "static-seed-v1",
                RoleType         = roleType,
                IsSystemRole     = true
            };

            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
                logger.LogError("RoleSeeder: failed to create {Role}: {Errors}", name,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        logger.LogInformation("Role seed complete: {Count} system role(s) catalogued", SystemRoles.Length);
    }
}
