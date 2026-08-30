using Faaz.Services.Administration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Administration.Infrastructure.DatabaseContext.Configurations;

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SrNo).ValueGeneratedNever();

        builder.HasQueryFilter(x => !x.IsDeleted);

        // Name stays bounded — it carries a lookup index below, and SQL Server does not allow
        // nvarchar(max) as an index key column.
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => x.Name).HasFilter("[IsDeleted] = 0");

        builder.Property(x => x.HecosCode).HasMaxLength(20);
        builder.Property(x => x.DataSource).HasMaxLength(200);
        builder.Property(x => x.SourceUrl).HasMaxLength(500);

        builder.HasIndex(x => x.HecosCode).HasFilter("[IsDeleted] = 0 AND [HecosCode] IS NOT NULL");
    }
}
