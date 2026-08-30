using Faaz.Services.Administration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Administration.Infrastructure.DatabaseContext.Configurations;

internal sealed class UniversityConfiguration : IEntityTypeConfiguration<University>
{
    public void Configure(EntityTypeBuilder<University> builder)
    {
        builder.ToTable("Universities");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SrNo).ValueGeneratedNever();

        builder.HasQueryFilter(x => !x.IsDeleted);

        // Name stays bounded — it carries a lookup index below, and SQL Server does not allow
        // nvarchar(max) as an index key column.
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => x.Name).HasFilter("[IsDeleted] = 0");

        builder.Property(x => x.Ukprn).HasMaxLength(8);
        builder.Property(x => x.Nation).HasMaxLength(50);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.InstitutionType).HasMaxLength(50);
        builder.Property(x => x.WebsiteUrl).HasMaxLength(500);
        builder.Property(x => x.DataSource).HasMaxLength(200);
        builder.Property(x => x.SourceUrl).HasMaxLength(500);

        // Filtered, non-unique on purpose — a handful of legitimate rows (manually-added,
        // non-UK, pre-verification) will share a null Ukprn.
        builder.HasIndex(x => x.Ukprn).HasFilter("[IsDeleted] = 0 AND [Ukprn] IS NOT NULL");
    }
}
