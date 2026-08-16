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


        builder.HasIndex(g => g.StudentProfileId).IsUnique().HasDatabaseName("IX_StudentPostgraduateData_StudentProfileId");
    }
}
