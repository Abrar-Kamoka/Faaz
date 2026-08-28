using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Student.Domain.Entities;
using Faaz.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
// Aliased: these entity type names collide with this class's own same-named DbSet properties below,
// which would otherwise shadow the type in nameof(...) member-access expressions.
using SixthFormDataEntity = Faaz.Services.Student.Domain.Entities.SixthFormData;
using UndergraduateDataEntity = Faaz.Services.Student.Domain.Entities.UndergraduateData;
using PostgraduateDataEntity = Faaz.Services.Student.Domain.Entities.PostgraduateData;

namespace Faaz.Services.Student.Infrastructure.DatabaseContext;

public class StudentDbContext : DbContext
{
    public StudentDbContext(DbContextOptions<StudentDbContext> options) : base(options) { }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ConfigureUtcDateTimeConvention();
        base.ConfigureConventions(configurationBuilder);
    }

    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<SixthFormData> SixthFormData => Set<SixthFormData>();
    public DbSet<UndergraduateData> UndergraduateData => Set<UndergraduateData>();
    public DbSet<PostgraduateData> PostgraduateData => Set<PostgraduateData>();
    public DbSet<SavedConsultant> SavedConsultants => Set<SavedConsultant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("student");

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

        builder.Entity<StudentProfile>().ApplyStandardColumnOrder(
            nameof(StudentProfile.UserId), nameof(StudentProfile.Email), nameof(StudentProfile.FirstName),
            nameof(StudentProfile.LastName), nameof(StudentProfile.DateOfBirth), nameof(StudentProfile.CountryOfCitizenship),
            nameof(StudentProfile.CountryOfResidence), nameof(StudentProfile.Ethnicity), nameof(StudentProfile.FirstLanguage),
            nameof(StudentProfile.AdditionalLanguages), nameof(StudentProfile.StudyTrack), nameof(StudentProfile.TargetStudyLevel),
            nameof(StudentProfile.TargetSubjects), nameof(StudentProfile.TargetUniversities), nameof(StudentProfile.HelpTypes),
            nameof(StudentProfile.ProfilePhotoUrl), nameof(StudentProfile.Bio), nameof(StudentProfile.ProfileCompleteness),
            nameof(StudentProfile.IsOnboardingComplete));

        builder.Entity<SixthFormData>().ApplyStandardColumnOrder(
            nameof(SixthFormDataEntity.StudentProfileId), nameof(SixthFormDataEntity.School), nameof(SixthFormDataEntity.ExamBoard),
            nameof(SixthFormDataEntity.Subjects), nameof(SixthFormDataEntity.PredictedGrades), nameof(SixthFormDataEntity.TargetEntryYear));

        builder.Entity<UndergraduateData>().ApplyStandardColumnOrder(
            nameof(UndergraduateDataEntity.StudentProfileId), nameof(UndergraduateDataEntity.CurrentUniversity),
            nameof(UndergraduateDataEntity.DegreeSubject), nameof(UndergraduateDataEntity.YearOfStudy),
            nameof(UndergraduateDataEntity.CurrentGrade), nameof(UndergraduateDataEntity.IsGapYear));

        builder.Entity<PostgraduateData>().ApplyStandardColumnOrder(
            nameof(PostgraduateDataEntity.StudentProfileId), nameof(PostgraduateDataEntity.UndergraduateUniversity),
            nameof(PostgraduateDataEntity.UndergraduateDegree), nameof(PostgraduateDataEntity.UndergraduateGrade),
            nameof(PostgraduateDataEntity.PostgraduateStatus), nameof(PostgraduateDataEntity.ResearchInterests));

        builder.Entity<SavedConsultant>().ApplyStandardColumnOrder(
            nameof(SavedConsultant.StudentUserId), nameof(SavedConsultant.ConsultantUserId));
    }
}
