using Faaz.BuildingBlocks.Persistence;
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

        builder.HasDefaultSchema("consultant");

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

        builder.Entity<ConsultantApplication>().ApplyStandardColumnOrder(
            nameof(ConsultantApplication.UserId), nameof(ConsultantApplication.Email), nameof(ConsultantApplication.FirstName),
            nameof(ConsultantApplication.LastName), nameof(ConsultantApplication.PhoneNumber), nameof(ConsultantApplication.DateOfBirth),
            nameof(ConsultantApplication.Nationality), nameof(ConsultantApplication.CountryOfResidence), nameof(ConsultantApplication.IsUkBased),
            nameof(ConsultantApplication.CurrentRole), nameof(ConsultantApplication.Institution), nameof(ConsultantApplication.ExpertiseArea),
            nameof(ConsultantApplication.YearsOfExperience), nameof(ConsultantApplication.HighestQualification), nameof(ConsultantApplication.PrimaryLanguage),
            nameof(ConsultantApplication.LinkedInProfileUrl), nameof(ConsultantApplication.PersonalStatement), nameof(ConsultantApplication.ConsultationMode),
            nameof(ConsultantApplication.ReferralSource), nameof(ConsultantApplication.ApplicationStatus), nameof(ConsultantApplication.SubmittedAt),
            nameof(ConsultantApplication.AdminNotes), nameof(ConsultantApplication.Remarks), nameof(ConsultantApplication.SetupInviteToken),
            nameof(ConsultantApplication.SetupInviteTokenExpiry), nameof(ConsultantApplication.SetupInviteSentAt));

        builder.Entity<ConsultantApplicationDocument>().ApplyStandardColumnOrder(
            nameof(ConsultantApplicationDocument.ApplicationId), nameof(ConsultantApplicationDocument.DocumentType),
            nameof(ConsultantApplicationDocument.FileName), nameof(ConsultantApplicationDocument.FilePath),
            nameof(ConsultantApplicationDocument.ContentType), nameof(ConsultantApplicationDocument.FileSizeBytes),
            nameof(ConsultantApplicationDocument.UploadedAt));

        builder.Entity<ConsultantProfile>().ApplyStandardColumnOrder(
            nameof(ConsultantProfile.UserId), nameof(ConsultantProfile.ApplicationId), nameof(ConsultantProfile.FullLegalName),
            nameof(ConsultantProfile.DisplayName), nameof(ConsultantProfile.ProfessionalPhotoUrl), nameof(ConsultantProfile.CurrentRole),
            nameof(ConsultantProfile.Institution), nameof(ConsultantProfile.LinkedInUrl), nameof(ConsultantProfile.YearsOfExperience),
            nameof(ConsultantProfile.StudyLevelsOffered), nameof(ConsultantProfile.SubjectAreas), nameof(ConsultantProfile.SpecialisedUniversities),
            nameof(ConsultantProfile.ServicesOffered), nameof(ConsultantProfile.WrittenBio), nameof(ConsultantProfile.IntroVideoUrl),
            nameof(ConsultantProfile.CallPreference), nameof(ConsultantProfile.MinBookingNoticeHours), nameof(ConsultantProfile.MaxAdvanceBookingDays),
            nameof(ConsultantProfile.IsProfileComplete), nameof(ConsultantProfile.IsActive), nameof(ConsultantProfile.IsFeatured),
            nameof(ConsultantProfile.StripeAccountId), nameof(ConsultantProfile.IsStripeDetailsSubmitted), nameof(ConsultantProfile.IsStripeChargesEnabled));

        builder.Entity<ConsultantSessionType>().ApplyStandardColumnOrder(
            nameof(ConsultantSessionType.ConsultantProfileId), nameof(ConsultantSessionType.Name), nameof(ConsultantSessionType.Description),
            nameof(ConsultantSessionType.DurationMinutes), nameof(ConsultantSessionType.PriceGbp), nameof(ConsultantSessionType.SortOrder),
            nameof(ConsultantSessionType.IsActive));

        builder.Entity<ConsultantAvailabilitySlot>().ApplyStandardColumnOrder(
            nameof(ConsultantAvailabilitySlot.ConsultantProfileId), nameof(ConsultantAvailabilitySlot.IsBlockedDate),
            nameof(ConsultantAvailabilitySlot.DayOfWeek), nameof(ConsultantAvailabilitySlot.StartTimeUtc),
            nameof(ConsultantAvailabilitySlot.EndTimeUtc), nameof(ConsultantAvailabilitySlot.Date), nameof(ConsultantAvailabilitySlot.Reason));

        builder.Entity<ConsultantCredential>().ApplyStandardColumnOrder(
            nameof(ConsultantCredential.ConsultantProfileId), nameof(ConsultantCredential.FileName), nameof(ConsultantCredential.StoredPath),
            nameof(ConsultantCredential.ContentType), nameof(ConsultantCredential.FileSizeBytes), nameof(ConsultantCredential.UploadedAt));
    }
}
