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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(StudentDbContext).Assembly);

        // Configure SrNo as DB IDENTITY for all BaseEntity descendants.
        foreach (var entity in builder.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
        {
            builder.Entity(entity.ClrType)
                   .Property(nameof(BaseEntity.SrNo))
                   .ValueGeneratedOnAdd();
        }

        // Global soft-delete filters — deleted records are invisible to all queries.
        builder.Entity<StudentProfile>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<SixthFormData>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<UndergraduateData>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<PostgraduateData>().HasQueryFilter(e => !e.IsDeleted);
    }
}
