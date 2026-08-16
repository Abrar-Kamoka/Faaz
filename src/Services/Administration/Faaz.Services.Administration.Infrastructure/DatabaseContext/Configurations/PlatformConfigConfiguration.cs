using Faaz.Services.Administration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Administration.Infrastructure.DatabaseContext.Configurations;

internal sealed class PlatformConfigConfiguration : IEntityTypeConfiguration<PlatformConfig>
{
    public void Configure(EntityTypeBuilder<PlatformConfig> builder)
    {
        builder.ToTable("PlatformConfigs", "admin");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SrNo).ValueGeneratedNever();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.LastUpdatedAt).IsRequired();
        builder.Property(x => x.LastUpdatedByAdminId).IsRequired();

        builder.HasIndex(x => x.Key).IsUnique()
               .HasFilter("[IsDeleted] = 0");
    }
}
