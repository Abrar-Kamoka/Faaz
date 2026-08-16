using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Payment.Infrastructure.DatabaseContext.Configurations
{
    using Payment = global::Faaz.Services.Payment.Domain.Entities.Payment;

    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SrNo).ValueGeneratedNever();
            builder.HasIndex(x => x.SrNo).IsUnique();
            builder.HasIndex(x => x.BookingId);
            builder.HasIndex(x => x.StripePaymentIntentId).IsUnique();
            // StripePaymentIntentId stays bounded — it carries a unique index above, and SQL Server
            // does not allow nvarchar(max) as an index key column.
            builder.Property(x => x.StripePaymentIntentId).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Amount).HasColumnType("decimal(10,2)");
            builder.Property(x => x.PlatformFee).HasColumnType("decimal(10,2)");
            builder.Property(x => x.ConsultantPayout).HasColumnType("decimal(10,2)");
            builder.Property(x => x.DiscountAmount).HasColumnType("decimal(10,2)");
            builder.Property(x => x.Status).HasConversion<int>();

            builder.HasQueryFilter(x => !x.IsDeleted);
            builder.HasMany(x => x.Refunds).WithOne(r => r.Payment).HasForeignKey(r => r.PaymentId);
        }
    }
}
