using Faaz.Services.Payment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Payment.Infrastructure.DatabaseContext.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SrNo).ValueGeneratedNever();
        builder.HasIndex(x => x.SrNo).IsUnique();
        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => x.StripeRefundId);
        builder.Property(x => x.StripeRefundId).HasMaxLength(200);
        builder.Property(x => x.Amount).HasColumnType("decimal(10,2)");
        builder.Property(x => x.Currency).HasMaxLength(10);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Remarks).HasMaxLength(2000);
        builder.Property(x => x.ExtraField1).HasMaxLength(500);
        builder.Property(x => x.ExtraField2).HasMaxLength(500);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
