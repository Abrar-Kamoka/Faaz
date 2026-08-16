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

        builder.Property(p => p.FullLegalName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.ProfessionalPhotoUrl).HasMaxLength(500);
        builder.Property(p => p.CurrentRole).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Institution).HasMaxLength(200).IsRequired();
        builder.Property(p => p.LinkedInUrl).HasMaxLength(500);
        builder.Property(p => p.WrittenBio).HasMaxLength(2000);
        builder.Property(p => p.IntroVideoUrl).HasMaxLength(500);
        builder.Property(p => p.StripeAccountId).HasMaxLength(100);
        builder.Property(p => p.CallPreference).HasConversion<int>();
        builder.Property(p => p.Remarks).HasMaxLength(500);
        builder.Property(p => p.ExtraField1).HasMaxLength(500);
        builder.Property(p => p.ExtraField2).HasMaxLength(500);

        builder.Property(p => p.StudyLevelsOffered).HasColumnType("nvarchar(max)");
        builder.Property(p => p.SubjectAreas).HasColumnType("nvarchar(max)");
        builder.Property(p => p.SpecialisedUniversities).HasColumnType("nvarchar(max)");
        builder.Property(p => p.ServicesOffered).HasColumnType("nvarchar(max)");

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
    }
}
