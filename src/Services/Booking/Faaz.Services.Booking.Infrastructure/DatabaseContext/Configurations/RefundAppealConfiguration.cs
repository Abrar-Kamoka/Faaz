using Faaz.Services.Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Booking.Infrastructure.DatabaseContext.Configurations;

public class RefundAppealConfiguration : IEntityTypeConfiguration<RefundAppeal>
{
    public void Configure(EntityTypeBuilder<RefundAppeal> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).IsRequired();
        builder.Property(x => x.RequestedAmountGbp).HasColumnType("decimal(10,2)");

        // No HasQueryFilter — appeals are audit records, never soft-deleted
        builder.HasIndex(x => x.BookingId).IsUnique(); // one appeal per booking
        builder.HasIndex(x => x.Status);               // admin queries by Pending

        builder.HasOne(x => x.Booking)
               .WithOne(x => x.RefundAppeal)
               .HasForeignKey<RefundAppeal>(x => x.BookingId)
               .OnDelete(DeleteBehavior.Restrict); // preserve appeal even if booking is soft-deleted
    }
}
