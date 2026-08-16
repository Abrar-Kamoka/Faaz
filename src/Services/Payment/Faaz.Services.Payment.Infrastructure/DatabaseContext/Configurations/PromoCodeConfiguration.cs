using Faaz.Services.Payment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Payment.Infrastructure.DatabaseContext.Configurations;

public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SrNo).ValueGeneratedNever();
        builder.HasIndex(x => x.SrNo).IsUnique();
        builder.HasIndex(x => x.Code).IsUnique();
        // Code stays bounded — it carries a unique index below, and SQL Server does not allow
        // nvarchar(max) as an index key column.
        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DiscountType).HasConversion<int>();
        builder.Property(x => x.DiscountValue).HasColumnType("decimal(10,2)");
        builder.Property(x => x.MaxDiscountAmount).HasColumnType("decimal(10,2)");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
