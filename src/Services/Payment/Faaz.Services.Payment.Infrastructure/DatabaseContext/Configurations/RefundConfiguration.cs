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
        // StripeRefundId stays bounded — it carries a lookup index below, and SQL Server does not
        // allow nvarchar(max) as an index key column.
        builder.Property(x => x.StripeRefundId).HasMaxLength(200);
        builder.Property(x => x.Amount).HasColumnType("decimal(10,2)");
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Reason).IsRequired();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
