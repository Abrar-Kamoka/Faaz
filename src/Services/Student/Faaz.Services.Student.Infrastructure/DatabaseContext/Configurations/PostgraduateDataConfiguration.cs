using Faaz.Services.Student.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Student.Infrastructure.DatabaseContext.Configurations;

internal sealed class PostgraduateDataConfiguration : IEntityTypeConfiguration<PostgraduateData>
{
    public void Configure(EntityTypeBuilder<PostgraduateData> builder)
    {
        builder.ToTable("StudentPostgraduateData");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.UndergraduateUniversity).HasMaxLength(200);
        builder.Property(g => g.UndergraduateDegree).HasMaxLength(200);
        builder.Property(g => g.UndergraduateGrade).HasMaxLength(50);
        builder.Property(g => g.PostgraduateStatus).HasMaxLength(100);
        builder.Property(g => g.ResearchInterests).HasMaxLength(1000);
        builder.Property(g => g.Remarks).HasMaxLength(500);
        builder.Property(g => g.ExtraField1).HasMaxLength(500);
        builder.Property(g => g.ExtraField2).HasMaxLength(500);

        builder.HasIndex(g => g.StudentProfileId).IsUnique().HasDatabaseName("IX_StudentPostgraduateData_StudentProfileId");
    }
}
