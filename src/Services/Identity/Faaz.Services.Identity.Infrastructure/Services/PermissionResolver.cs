using Faaz.Services.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using static Faaz.Services.Identity.Domain.IdentityEnums;

namespace Faaz.Services.Identity.Infrastructure.Services;

// Only SuperAdmin-tier accounts ever get a "permission" claim — Student/Consultant authorization keeps
// running entirely off the "role" claim and never queries roles/claims at all.
public static class PermissionResolver
{
    public static async Task<List<string>> GetPermissionsAsync(
        UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, ApplicationUser user)
    {
        if (user.Role != UserRole.SuperAdmin) return [];

        var roleNames = await userManager.GetRolesAsync(user);
        var permissions = new HashSet<string>();

        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var claims = await roleManager.GetClaimsAsync(role);
            foreach (var claim in claims)
                if (claim.Type == "permission")
                    permissions.Add(claim.Value);
        }

        return [.. permissions];
    }
}
