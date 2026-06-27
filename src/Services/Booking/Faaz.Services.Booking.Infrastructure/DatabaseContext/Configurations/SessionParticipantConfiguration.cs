using Faaz.Services.Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Booking.Infrastructure.DatabaseContext.Configurations;

public class SessionParticipantConfiguration : IEntityTypeConfiguration<SessionParticipant>
{
    public void Configure(EntityTypeBuilder<SessionParticipant> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PendingReconnectionJobId).HasMaxLength(100);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.ExtraField1).HasMaxLength(500);
        builder.Property(x => x.ExtraField2).HasMaxLength(500);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.SessionId);
        builder.HasIndex(x => new { x.SessionId, x.UserId }).IsUnique();
    }
}
