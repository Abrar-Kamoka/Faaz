using Faaz.Services.Consultant.Domain.Entities;
using Faaz.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Consultant.Infrastructure.DatabaseContext;

public class ConsultantDbContext : DbContext
{
    public ConsultantDbContext(DbContextOptions<ConsultantDbContext> options) : base(options) { }

    public DbSet<ConsultantApplication> ConsultantApplications => Set<ConsultantApplication>();
    public DbSet<ConsultantApplicationDocument> ConsultantApplicationDocuments => Set<ConsultantApplicationDocument>();
    public DbSet<ConsultantProfile> ConsultantProfiles => Set<ConsultantProfile>();
    public DbSet<ConsultantSessionType> ConsultantSessionTypes => Set<ConsultantSessionType>();
    public DbSet<ConsultantAvailabilitySlot> ConsultantAvailabilitySlots => Set<ConsultantAvailabilitySlot>();
    public DbSet<ConsultantCredential> ConsultantCredentials => Set<ConsultantCredential>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ConsultantDbContext).Assembly);

        // SrNo is managed by application code (NewSerialNumberAsync → MAX+1), not by the database.
        foreach (var entity in builder.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
        {
            builder.Entity(entity.ClrType)
                   .Property(nameof(BaseEntity.SrNo))
                   .ValueGeneratedNever();
        }

        // Global soft-delete filters — deleted records are invisible to all queries.
        builder.Entity<ConsultantApplication>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ConsultantProfile>().HasQueryFilter(e => !e.IsDeleted);
        // ConsultantSessionType uses hard delete — no query filter.
        builder.Entity<ConsultantAvailabilitySlot>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ConsultantCredential>().HasQueryFilter(e => !e.IsDeleted);
    }
}
