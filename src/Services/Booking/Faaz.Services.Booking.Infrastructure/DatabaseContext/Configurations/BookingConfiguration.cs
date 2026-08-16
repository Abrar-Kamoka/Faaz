using Faaz.Services.Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Booking.Infrastructure.DatabaseContext.Configurations
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;

    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SessionTypeName).IsRequired();
            builder.Property(x => x.SessionPriceGbp).HasColumnType("decimal(10,2)");
            builder.Property(x => x.PlatformCommissionGbp).HasColumnType("decimal(10,2)");
            builder.Property(x => x.PromoDiscountGbp).HasColumnType("decimal(10,2)");
            builder.Property(x => x.TotalChargedGbp).HasColumnType("decimal(10,2)");
            builder.Property(x => x.StudentTimezone).IsRequired();

            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasIndex(x => new { x.ConsultantUserId, x.Status, x.ScheduledStartUtc });
            builder.HasIndex(x => new { x.StudentUserId, x.Status, x.ScheduledStartUtc });
            builder.HasIndex(x => new { x.Status, x.ExpiresAt });

            // Double-booking prevention: one active booking per consultant slot
            builder.HasIndex(x => new { x.ConsultantProfileId, x.ScheduledStartUtc })
                .IsUnique()
                .HasFilter("[Status] IN (0, 1, 2, 3)"); // SlotReserved, PendingConfirmation, Confirmed, InProgress

            builder.HasMany(x => x.StatusHistory)
                .WithOne(x => x.Booking)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Session)
                .WithOne(x => x.Booking)
                .HasForeignKey<Session>(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Review)
                .WithOne(x => x.Booking)
                .HasForeignKey<Review>(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
