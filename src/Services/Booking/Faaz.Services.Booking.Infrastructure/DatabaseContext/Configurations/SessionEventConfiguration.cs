using Faaz.Services.Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Booking.Infrastructure.DatabaseContext.Configurations;

public class SessionEventConfiguration : IEntityTypeConfiguration<SessionEvent>
{
    public void Configure(EntityTypeBuilder<SessionEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LiveKitRoomSid).IsRequired();
        // LiveKitEventId stays bounded — it carries a unique index below, and SQL Server does not
        // allow nvarchar(max) as an index key column.
        builder.Property(x => x.LiveKitEventId).HasMaxLength(100).IsRequired();

        // No HasQueryFilter — append-only evidence table
        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => x.LiveKitEventId).IsUnique(); // idempotency
    }
}
