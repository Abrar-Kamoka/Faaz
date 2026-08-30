using Faaz.Services.Student.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Student.Infrastructure.DatabaseContext.Configurations;

internal sealed class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("StudentProfiles");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Email).IsRequired();
        builder.Property(p => p.FirstName).IsRequired();
        builder.Property(p => p.LastName).IsRequired();

        builder.Property(p => p.AdditionalLanguages).HasColumnType("nvarchar(max)");

        builder.HasIndex(p => p.UserId).IsUnique().HasDatabaseName("IX_StudentProfiles_UserId");
        builder.Property(p => p.UserId).IsRequired();

        builder.HasOne(p => p.SixthFormData)
            .WithOne(s => s.StudentProfile)
            .HasForeignKey<SixthFormData>(s => s.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.UndergraduateData)
            .WithOne(u => u.StudentProfile)
            .HasForeignKey<UndergraduateData>(u => u.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.PostgraduateData)
            .WithOne(g => g.StudentProfile)
            .HasForeignKey<PostgraduateData>(g => g.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.HelpServices).WithOne(s => s.StudentProfile)
               .HasForeignKey(s => s.StudentProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.TargetUniversities).WithOne(s => s.StudentProfile)
               .HasForeignKey(s => s.StudentProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.TargetSubjects).WithOne(s => s.StudentProfile)
               .HasForeignKey(s => s.StudentProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.TargetProgrammes).WithOne(s => s.StudentProfile)
               .HasForeignKey(s => s.StudentProfileId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class StudentProfileHelpServiceConfiguration : IEntityTypeConfiguration<StudentProfileHelpService>
{
    public void Configure(EntityTypeBuilder<StudentProfileHelpService> builder)
    {
        builder.ToTable("StudentProfileHelpServices");
        builder.HasKey(x => new { x.StudentProfileId, x.ServiceId });
    }
}

internal sealed class StudentProfileTargetUniversityConfiguration : IEntityTypeConfiguration<StudentProfileTargetUniversity>
{
    public void Configure(EntityTypeBuilder<StudentProfileTargetUniversity> builder)
    {
        builder.ToTable("StudentProfileTargetUniversities");
        builder.HasKey(x => new { x.StudentProfileId, x.UniversityId });
    }
}

internal sealed class StudentProfileTargetSubjectConfiguration : IEntityTypeConfiguration<StudentProfileTargetSubject>
{
    public void Configure(EntityTypeBuilder<StudentProfileTargetSubject> builder)
    {
        builder.ToTable("StudentProfileTargetSubjects");
        builder.HasKey(x => new { x.StudentProfileId, x.SubjectId });
    }
}

internal sealed class StudentProfileTargetProgrammeConfiguration : IEntityTypeConfiguration<StudentProfileTargetProgramme>
{
    public void Configure(EntityTypeBuilder<StudentProfileTargetProgramme> builder)
    {
        builder.ToTable("StudentProfileTargetProgrammes");
        builder.HasKey(x => new { x.StudentProfileId, x.ProgrammeId });
    }
}
