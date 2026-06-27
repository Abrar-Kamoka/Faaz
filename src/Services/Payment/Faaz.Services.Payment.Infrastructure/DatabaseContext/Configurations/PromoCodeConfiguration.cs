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
        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DiscountType).HasConversion<int>();
        builder.Property(x => x.DiscountValue).HasColumnType("decimal(10,2)");
        builder.Property(x => x.MaxDiscountAmount).HasColumnType("decimal(10,2)");
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Remarks).HasMaxLength(2000);
        builder.Property(x => x.ExtraField1).HasMaxLength(500);
        builder.Property(x => x.ExtraField2).HasMaxLength(500);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
