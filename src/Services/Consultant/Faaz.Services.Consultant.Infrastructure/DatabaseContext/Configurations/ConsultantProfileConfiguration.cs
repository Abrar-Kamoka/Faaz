using Faaz.Services.Consultant.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Consultant.Infrastructure.DatabaseContext.Configurations;

internal sealed class ConsultantProfileConfiguration : IEntityTypeConfiguration<ConsultantProfile>
{
    public void Configure(EntityTypeBuilder<ConsultantProfile> builder)
    {
        builder.ToTable("ConsultantProfiles");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FullLegalName).IsRequired();
        builder.Property(p => p.DisplayName).IsRequired();
        builder.Property(p => p.CurrentRole).IsRequired();
        builder.Property(p => p.Institution).IsRequired();
        builder.Property(p => p.CallPreference).HasConversion<int>();

        builder.Property(p => p.StudyLevelsOffered).HasColumnType("nvarchar(max)");

        builder.Property(p => p.IsFeatured).HasDefaultValue(false);

        builder.HasIndex(p => p.UserId).IsUnique().HasDatabaseName("IX_ConsultantProfiles_UserId");
        builder.HasIndex(p => p.ApplicationId).IsUnique().HasDatabaseName("IX_ConsultantProfiles_ApplicationId");
        builder.HasIndex(p => new { p.IsActive, p.IsProfileComplete }).HasDatabaseName("IX_ConsultantProfiles_IsActive_IsProfileComplete");

        builder.HasOne(p => p.Application)
               .WithOne(a => a.Profile)
               .HasForeignKey<ConsultantProfile>(p => p.ApplicationId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_ConsultantProfiles_ConsultantApplications_ApplicationId");

        builder.HasMany(p => p.SessionTypes).WithOne(s => s.Profile)
               .HasForeignKey(s => s.ConsultantProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.AvailabilitySlots).WithOne(s => s.Profile)
               .HasForeignKey(s => s.ConsultantProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Services).WithOne(s => s.Profile)
               .HasForeignKey(s => s.ConsultantProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Subjects).WithOne(s => s.Profile)
               .HasForeignKey(s => s.ConsultantProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Universities).WithOne(s => s.Profile)
               .HasForeignKey(s => s.ConsultantProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ConsultantProfileServiceConfiguration : IEntityTypeConfiguration<ConsultantProfileService>
{
    public void Configure(EntityTypeBuilder<ConsultantProfileService> builder)
    {
        builder.ToTable("ConsultantProfileServices");
        builder.HasKey(x => new { x.ConsultantProfileId, x.ServiceId });
    }
}

internal sealed class ConsultantProfileSubjectConfiguration : IEntityTypeConfiguration<ConsultantProfileSubject>
{
    public void Configure(EntityTypeBuilder<ConsultantProfileSubject> builder)
    {
        builder.ToTable("ConsultantProfileSubjects");
        builder.HasKey(x => new { x.ConsultantProfileId, x.SubjectId });
    }
}

internal sealed class ConsultantProfileUniversityConfiguration : IEntityTypeConfiguration<ConsultantProfileUniversity>
{
    public void Configure(EntityTypeBuilder<ConsultantProfileUniversity> builder)
    {
        builder.ToTable("ConsultantProfileUniversities");
        builder.HasKey(x => new { x.ConsultantProfileId, x.UniversityId });
    }
}
