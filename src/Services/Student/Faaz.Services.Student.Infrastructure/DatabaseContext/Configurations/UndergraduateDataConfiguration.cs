using Faaz.Services.Student.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Student.Infrastructure.DatabaseContext.Configurations;

internal sealed class UndergraduateDataConfiguration : IEntityTypeConfiguration<UndergraduateData>
{
    public void Configure(EntityTypeBuilder<UndergraduateData> builder)
    {
        builder.ToTable("StudentUndergraduateData");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.CurrentUniversity).HasMaxLength(200);
        builder.Property(u => u.DegreeSubject).HasMaxLength(200);
        builder.Property(u => u.CurrentGrade).HasMaxLength(50);
        builder.Property(u => u.Remarks).HasMaxLength(500);
        builder.Property(u => u.ExtraField1).HasMaxLength(500);
        builder.Property(u => u.ExtraField2).HasMaxLength(500);

        builder.HasIndex(u => u.StudentProfileId).IsUnique().HasDatabaseName("IX_StudentUndergraduateData_StudentProfileId");
    }
}
