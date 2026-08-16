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


        builder.HasIndex(u => u.StudentProfileId).IsUnique().HasDatabaseName("IX_StudentUndergraduateData_StudentProfileId");
    }
}
