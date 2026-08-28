using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Identity.Domain.Entities;
using Faaz.SharedKernel.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Identity.Infrastructure.DatabaseContext;

public sealed class IdentityDbContext : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    Guid,
    IdentityUserClaim<Guid>,
    ApplicationUserRole,
    IdentityUserLogin<Guid>,
    IdentityRoleClaim<Guid>,
    ApplicationUserToken>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ConfigureUtcDateTimeConvention();
        base.ConfigureConventions(configurationBuilder);
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Permission> Permissions => Set<Permission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("identity");

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<ApplicationUserRole>().ToTable("UserRoles");
        builder.Entity<ApplicationUserToken>().ToTable("UserTokens");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // SrNo is managed by application code (NewSerialNumberAsync → MAX+1), not by the database.
        foreach (var entity in builder.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
        {
            builder.Entity(entity.ClrType)
                   .Property(nameof(BaseEntity.SrNo))
                   .ValueGeneratedNever();
        }

        // ApplicationUser has its own SrNo (not inherited from BaseEntity) — configure separately.
        builder.Entity<ApplicationUser>().Property(u => u.SrNo).ValueGeneratedNever();

        // System roles (SuperAdmin/Student/Consultant) are seeded at runtime by RoleSeeder, not here —
        // HasData only inserts once, at first migration-apply, and never recovers from a data wipe.
        builder.Entity<ApplicationUser>().ApplyStandardColumnOrder(
            nameof(ApplicationUser.UserName), nameof(ApplicationUser.FirstName), nameof(ApplicationUser.LastName),
            nameof(ApplicationUser.Email), nameof(ApplicationUser.EmailConfirmed), nameof(ApplicationUser.IsEmailVerified),
            nameof(ApplicationUser.EmailVerificationToken), nameof(ApplicationUser.EmailVerificationTokenExpiry),
            nameof(ApplicationUser.PhoneNumber), nameof(ApplicationUser.PhoneNumberConfirmed), nameof(ApplicationUser.PasswordHash),
            nameof(ApplicationUser.LastLoginAt), nameof(ApplicationUser.Role), nameof(ApplicationUser.Status),
            nameof(ApplicationUser.ConsultantApplicationStatus), nameof(ApplicationUser.TwoFactorEnabled),
            nameof(ApplicationUser.LockoutEnabled), nameof(ApplicationUser.LockoutEnd), nameof(ApplicationUser.AccessFailedCount),
            nameof(ApplicationUser.EmailNotificationsEnabled), nameof(ApplicationUser.InAppNotificationsEnabled),
            nameof(ApplicationUser.Remarks), nameof(ApplicationUser.NormalizedUserName), nameof(ApplicationUser.NormalizedEmail),
            nameof(ApplicationUser.SecurityStamp), nameof(ApplicationUser.ConcurrencyStamp));

        builder.Entity<RefreshToken>().ApplyStandardColumnOrder(
            nameof(RefreshToken.CreatedByIp), nameof(RefreshToken.Token), nameof(RefreshToken.UserId),
            nameof(RefreshToken.ExpiresAt), nameof(RefreshToken.JwtId), nameof(RefreshToken.RememberMe),
            nameof(RefreshToken.IsUsed), nameof(RefreshToken.IsRevoked), nameof(RefreshToken.RevokedByIp),
            nameof(RefreshToken.ReplacedByToken));

        builder.Entity<PasswordResetToken>().ApplyStandardColumnOrder(
            nameof(PasswordResetToken.Token), nameof(PasswordResetToken.UserId), nameof(PasswordResetToken.ExpiresAt),
            nameof(PasswordResetToken.IsUsed));

        builder.Entity<Permission>().ApplyStandardColumnOrder(
            nameof(Permission.Key), nameof(Permission.Category), nameof(Permission.Description));
    }
}
