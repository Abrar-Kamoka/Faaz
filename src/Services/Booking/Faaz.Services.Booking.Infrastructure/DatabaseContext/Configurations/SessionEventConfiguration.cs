using Faaz.Services.Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Booking.Infrastructure.DatabaseContext.Configurations;

public class SessionEventConfiguration : IEntityTypeConfiguration<SessionEvent>
{
    public void Configure(EntityTypeBuilder<SessionEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LiveKitRoomSid).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LiveKitEventId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ParticipantIdentity).HasMaxLength(100);
        builder.Property(x => x.RawWebhookPayload).HasMaxLength(8000);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.ExtraField1).HasMaxLength(500);
        builder.Property(x => x.ExtraField2).HasMaxLength(500);

        // No HasQueryFilter — append-only evidence table
        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.LiveKitEventId).IsUnique(); // idempotency
    }
}
