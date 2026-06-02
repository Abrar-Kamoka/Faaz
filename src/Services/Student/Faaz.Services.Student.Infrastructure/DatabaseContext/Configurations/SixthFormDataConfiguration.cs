using Faaz.Services.Student.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Student.Infrastructure.DatabaseContext.Configurations;

internal sealed class SixthFormDataConfiguration : IEntityTypeConfiguration<SixthFormData>
{
    public void Configure(EntityTypeBuilder<SixthFormData> builder)
    {
        builder.ToTable("StudentSixthFormData");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ExamBoard).HasMaxLength(100);
        builder.Property(s => s.PredictedGrades).HasMaxLength(200);
        builder.Property(s => s.School).HasMaxLength(200);
        builder.Property(s => s.Remarks).HasMaxLength(500);
        builder.Property(s => s.ExtraField1).HasMaxLength(500);
        builder.Property(s => s.ExtraField2).HasMaxLength(500);
        builder.Property(s => s.Subjects).HasColumnType("nvarchar(max)");

        builder.HasIndex(s => s.StudentProfileId).IsUnique().HasDatabaseName("IX_StudentSixthFormData_StudentProfileId");
    }
}
