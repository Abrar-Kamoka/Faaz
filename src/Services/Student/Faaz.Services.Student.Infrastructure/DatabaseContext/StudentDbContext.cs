using Faaz.Services.Student.Domain.Entities;
using Faaz.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Student.Infrastructure.DatabaseContext;

public class StudentDbContext : DbContext
{
    public StudentDbContext(DbContextOptions<StudentDbContext> options) : base(options) { }

    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<SixthFormData> SixthFormData => Set<SixthFormData>();
    public DbSet<UndergraduateData> UndergraduateData => Set<UndergraduateData>();
    public DbSet<PostgraduateData> PostgraduateData => Set<PostgraduateData>();
    public DbSet<SavedConsultant> SavedConsultants => Set<SavedConsultant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(StudentDbContext).Assembly);

        // SrNo is managed by application code (NewSerialNumberAsync → MAX+1), not by the database.
        foreach (var entity in builder.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
        {
            builder.Entity(entity.ClrType)
                   .Property(nameof(BaseEntity.SrNo))
                   .ValueGeneratedNever();
        }

        // Global soft-delete filters — deleted records are invisible to all queries.
        builder.Entity<StudentProfile>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<SixthFormData>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<UndergraduateData>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PostgraduateData>().HasQueryFilter(e => !e.IsDeleted);
    }
}
