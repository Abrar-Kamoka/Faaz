using Faaz.Services.Administration.Domain.Entities;
using Faaz.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Administration.Infrastructure.DatabaseContext;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<AdminActionLog> AdminActionLogs { get; set; }
    public DbSet<PlatformConfig> PlatformConfigs { get; set; }
    public DbSet<DisputeNote>    DisputeNotes    { get; set; }
    public DbSet<University>     Universities    { get; set; }
    public DbSet<Subject>        Subjects        { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);

        foreach (var entity in builder.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
        {
            builder.Entity(entity.ClrType)
                   .Property(nameof(BaseEntity.SrNo))
                   .ValueGeneratedNever();
        }
    }

}
