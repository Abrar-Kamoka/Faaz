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
            builder.Property(x => x.StripePaymentIntentId).IsRequired().HasMaxLength(200);
            builder.Property(x => x.StripeChargeId).HasMaxLength(200);
            builder.Property(x => x.StripeCustomerId).HasMaxLength(200);
            builder.Property(x => x.Amount).HasColumnType("decimal(10,2)");
            builder.Property(x => x.PlatformFee).HasColumnType("decimal(10,2)");
            builder.Property(x => x.ConsultantPayout).HasColumnType("decimal(10,2)");
            builder.Property(x => x.DiscountAmount).HasColumnType("decimal(10,2)");
            builder.Property(x => x.Currency).HasMaxLength(10);
            builder.Property(x => x.PromoCodeUsed).HasMaxLength(100);
            builder.Property(x => x.Status).HasConversion<int>();
            builder.Property(x => x.Remarks).HasMaxLength(2000);
            builder.Property(x => x.ExtraField1).HasMaxLength(500);
            builder.Property(x => x.ExtraField2).HasMaxLength(500);

            builder.HasQueryFilter(x => !x.IsDeleted);
            builder.HasMany(x => x.Refunds).WithOne(r => r.Payment).HasForeignKey(r => r.PaymentId);
        }
    }
}
