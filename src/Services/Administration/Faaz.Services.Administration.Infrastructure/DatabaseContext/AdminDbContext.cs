using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Administration.Domain.Entities;
using Faaz.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Administration.Infrastructure.DatabaseContext;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ConfigureUtcDateTimeConvention();
        base.ConfigureConventions(configurationBuilder);
    }

    public DbSet<AdminActionLog>        AdminActionLogs        { get; set; }
    public DbSet<PlatformConfig>        PlatformConfigs        { get; set; }
    public DbSet<DisputeNote>           DisputeNotes           { get; set; }
    public DbSet<University>            Universities           { get; set; }
    public DbSet<Subject>               Subjects               { get; set; }
    public DbSet<Programme>             Programmes             { get; set; }
    public DbSet<ProgrammeSubject>      ProgrammeSubjects      { get; set; }
    public DbSet<Service>               Services               { get; set; }
    public DbSet<ReferenceDataRequest>  ReferenceDataRequests  { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("admin");

        builder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);

        foreach (var entity in builder.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
        {
            builder.Entity(entity.ClrType)
                   .Property(nameof(BaseEntity.SrNo))
                   .ValueGeneratedNever();
        }

        builder.Entity<AdminActionLog>().ApplyStandardColumnOrder(
            nameof(AdminActionLog.AdminUserId), nameof(AdminActionLog.Action), nameof(AdminActionLog.EntityType),
            nameof(AdminActionLog.EntityId), nameof(AdminActionLog.PerformedAt), nameof(AdminActionLog.IpAddress),
            nameof(AdminActionLog.Notes), nameof(AdminActionLog.BeforeJson), nameof(AdminActionLog.AfterJson));

        builder.Entity<DisputeNote>().ApplyStandardColumnOrder(
            nameof(DisputeNote.BookingId), nameof(DisputeNote.AuthorAdminId), nameof(DisputeNote.Content));

        builder.Entity<PlatformConfig>().ApplyStandardColumnOrder(
            nameof(PlatformConfig.Key), nameof(PlatformConfig.Value), nameof(PlatformConfig.Description),
            nameof(PlatformConfig.LastUpdatedAt), nameof(PlatformConfig.LastUpdatedByAdminId));

        builder.Entity<University>().ApplyStandardColumnOrder(
            nameof(University.Name), nameof(University.Ukprn), nameof(University.Country), nameof(University.Nation),
            nameof(University.City), nameof(University.InstitutionType), nameof(University.IsRussellGroup),
            nameof(University.LogoUrl), nameof(University.WebsiteUrl), nameof(University.IsActive),
            nameof(University.DataSource), nameof(University.SourceUrl), nameof(University.LastVerifiedAt));

        builder.Entity<Subject>().ApplyStandardColumnOrder(
            nameof(Subject.Name), nameof(Subject.HecosCode), nameof(Subject.Category), nameof(Subject.IsActive),
            nameof(Subject.DataSource), nameof(Subject.SourceUrl), nameof(Subject.LastVerifiedAt));

        builder.Entity<Programme>().ApplyStandardColumnOrder(
            nameof(Programme.UniversityId), nameof(Programme.Title), nameof(Programme.StudyLevel),
            nameof(Programme.Mode), nameof(Programme.DurationMonths), nameof(Programme.UcasCode),
            nameof(Programme.EntryRequirements), nameof(Programme.TuitionFeeDomesticGbp),
            nameof(Programme.TuitionFeeInternationalGbp), nameof(Programme.IsActive),
            nameof(Programme.DataSource), nameof(Programme.SourceUrl), nameof(Programme.LastVerifiedAt));

        builder.Entity<Service>().ApplyStandardColumnOrder(
            nameof(Service.Name), nameof(Service.Description), nameof(Service.Category),
            nameof(Service.SortOrder), nameof(Service.IsActive));

        builder.Entity<ReferenceDataRequest>().ApplyStandardColumnOrder(
            nameof(ReferenceDataRequest.RequestedByUserId), nameof(ReferenceDataRequest.RequestedByRole),
            nameof(ReferenceDataRequest.EntityType), nameof(ReferenceDataRequest.ProposedName),
            nameof(ReferenceDataRequest.Details), nameof(ReferenceDataRequest.Status),
            nameof(ReferenceDataRequest.ReviewedByAdminUserId), nameof(ReferenceDataRequest.ReviewNotes),
            nameof(ReferenceDataRequest.ReviewedAt));
    }

}
