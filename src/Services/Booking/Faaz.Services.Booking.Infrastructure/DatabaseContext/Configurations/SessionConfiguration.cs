using Faaz.Services.Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Booking.Infrastructure.DatabaseContext.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LiveKitRoomName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LiveKitRoomSid).HasMaxLength(100);
        builder.Property(x => x.CompletionPct).HasColumnType("decimal(5,2)");
        builder.Property(x => x.CreateRoomJobId).HasMaxLength(100);
        builder.Property(x => x.NoShowJobId).HasMaxLength(100);
        builder.Property(x => x.ForceCloseJobId).HasMaxLength(100);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.ExtraField1).HasMaxLength(500);
        builder.Property(x => x.ExtraField2).HasMaxLength(500);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.BookingId).IsUnique();
        builder.HasIndex(x => x.LiveKitRoomName);

        builder.HasMany(x => x.Participants)
            .WithOne(x => x.Session)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Events)
            .WithOne(x => x.Session)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
