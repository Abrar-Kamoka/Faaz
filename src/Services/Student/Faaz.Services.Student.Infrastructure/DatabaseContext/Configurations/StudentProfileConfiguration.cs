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
        builder.Property(p => p.HelpTypes).HasConversion<int>();

        builder.Property(p => p.AdditionalLanguages).HasColumnType("nvarchar(max)");
        builder.Property(p => p.TargetSubjects).HasColumnType("nvarchar(max)");
        builder.Property(p => p.TargetUniversities).HasColumnType("nvarchar(max)");

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
    }
}
