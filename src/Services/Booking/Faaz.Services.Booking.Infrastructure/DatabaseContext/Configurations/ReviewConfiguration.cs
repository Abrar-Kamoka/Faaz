using Faaz.Services.Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Booking.Infrastructure.DatabaseContext.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReviewText).HasMaxLength(2000);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.ExtraField1).HasMaxLength(500);
        builder.Property(x => x.ExtraField2).HasMaxLength(500);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.BookingId).IsUnique(); // one review per booking
        builder.HasIndex(x => new { x.ConsultantProfileId, x.IsPublic });
    }
}
