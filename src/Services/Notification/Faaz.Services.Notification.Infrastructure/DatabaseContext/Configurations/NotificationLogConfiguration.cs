using Faaz.Services.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Notification.Infrastructure.DatabaseContext.Configurations;

internal sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Payload).HasMaxLength(4000);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.ExtraField1).HasMaxLength(500);
        builder.Property(x => x.ExtraField2).HasMaxLength(500);
        builder.HasIndex(x => new { x.UserId, x.IsRead });
        builder.HasIndex(x => x.UserId);
    }
}
