using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.DatabaseContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Identity.WebHost.Seeding;

public static class RbacSeeder
{
    // The full permission catalog available to custom admin roles. Adding a row here makes it
    // assignable from Roles Management — it does nothing on its own until an [Authorize] policy
    // somewhere actually checks for it (see RequirePermission usages in Administration).
    public static readonly (string Key, string Category, string Description)[] Catalog =
    [
        ("bookings.view",              "Bookings",         "View all bookings"),
        ("bookings.resolve-dispute",   "Bookings",         "Resolve student/consultant disputes"),
        ("bookings.override-status",   "Bookings",         "Manually override a booking's status"),
        ("payments.view",              "Payments",         "View transactions and payouts"),
        ("payments.refund",            "Payments",         "Issue refunds on transactions"),
        ("consultants.view",           "Consultants",      "View consultant profiles and applications"),
        ("consultants.review",         "Consultants",      "Approve, reject, or suspend consultants"),
        ("users.view",                 "Users",            "View student and user accounts"),
        ("users.manage",               "Users",            "Deactivate or reactivate any account"),
        ("staff.manage",               "Staff & Roles",    "Create staff accounts and manage roles"),
        ("content.manage",             "Content",          "Manage universities, subjects, promo codes, and featured content"),
        ("reviews.moderate",           "Reviews",          "Hide, show, or delete reviews"),
    ];

    private static readonly Guid AdminSystemRoleId = Guid.Parse("b0044d0a-1f88-4957-953c-8b188a72aa02");

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        var db          = services.GetRequiredService<IdentityDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var (key, category, description) in Catalog)
        {
            if (await db.Permissions.AnyAsync(p => p.Key == key)) continue;
            db.Permissions.Add(new Permission { Key = key, Category = category, Description = description });
        }
        await db.SaveChangesAsync();

        // The built-in Admin role holds every permission by default — this is what preserves today's
        // "any admin can do anything" behaviour. Narrower access only happens once someone is moved
        // to a custom role created via Roles Management.
        var adminRole = await roleManager.FindByIdAsync(AdminSystemRoleId.ToString());
        if (adminRole is null)
        {
            logger.LogWarning("RbacSeeder: built-in Admin role not found — skipping permission claim seed");
            return;
        }

        var existingClaims = (await roleManager.GetClaimsAsync(adminRole))
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToHashSet();

        foreach (var (key, _, _) in Catalog)
        {
            if (existingClaims.Contains(key)) continue;
            await roleManager.AddClaimAsync(adminRole, new System.Security.Claims.Claim("permission", key));
        }

        logger.LogInformation("RBAC seed complete: {Count} permissions catalogued", Catalog.Length);
    }
}
