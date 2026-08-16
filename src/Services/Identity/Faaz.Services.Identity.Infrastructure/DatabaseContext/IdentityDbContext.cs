using Faaz.Services.Identity.Domain.Entities;
using Faaz.SharedKernel.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static Faaz.Services.Identity.Domain.IdentityEnums;
using static Faaz.SharedKernel.SharedEnums;

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

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Permission> Permissions => Set<Permission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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

        SeedAdminRole(builder);
    }

    private static void SeedAdminRole(ModelBuilder builder)
    {
        var adminRoleId = Guid.Parse("b0044d0a-1f88-4957-953c-8b188a72aa02");
        builder.Entity<ApplicationRole>().HasData(new ApplicationRole
        {
            Id = adminRoleId,
            Name = nameof(UserRole.Admin),
            NormalizedName = nameof(UserRole.Admin).ToUpperInvariant(),
            ConcurrencyStamp = "static-seed-v1",
            RoleType = UserRole.Admin,
            IsSystemRole = true
        });

        // Student/Consultant aren't ASP.NET Identity role assignments (that authorization still runs
        // entirely off ApplicationUser.Role), but they're seeded here too so Roles Management has a
        // complete, read-only picture of every role in the system, not just the admin-tier ones.
        builder.Entity<ApplicationRole>().HasData(new ApplicationRole
        {
            Id = Guid.Parse("c1155e1b-2f99-4a68-a64d-9c299b83bb03"),
            Name = nameof(UserRole.Student),
            NormalizedName = nameof(UserRole.Student).ToUpperInvariant(),
            ConcurrencyStamp = "static-seed-v1",
            RoleType = UserRole.Student,
            IsSystemRole = true
        });
        builder.Entity<ApplicationRole>().HasData(new ApplicationRole
        {
            Id = Guid.Parse("d2266f2c-3a00-4b79-b75e-ad3a0c94cc04"),
            Name = nameof(UserRole.Consultant),
            NormalizedName = nameof(UserRole.Consultant).ToUpperInvariant(),
            ConcurrencyStamp = "static-seed-v1",
            RoleType = UserRole.Consultant,
            IsSystemRole = true
        });
    }
}
