using Faaz.BuildingBlocks.Persistence;
using Faaz.Services.Notification.Domain.Entities;
using Faaz.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace Faaz.Services.Notification.Infrastructure.DatabaseContext;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<Announcement> Announcements => Set<Announcement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("notification");
        builder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);

        // SrNo is managed by application code (NewSerialNumberAsync → MAX+1), not by the database.
        foreach (var entity in builder.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType)))
        {
            builder.Entity(entity.ClrType)
                   .Property(nameof(BaseEntity.SrNo))
                   .ValueGeneratedNever();
        }

        builder.Entity<Announcement>().ApplyStandardColumnOrder(
            nameof(Announcement.Title), nameof(Announcement.Body), nameof(Announcement.Audience),
            nameof(Announcement.CreatedByAdminId), nameof(Announcement.IsActive), nameof(Announcement.PublishedAt),
            nameof(Announcement.ExpiresAt));

        builder.Entity<NotificationLog>().ApplyStandardColumnOrder(
            nameof(NotificationLog.UserId), nameof(NotificationLog.Channel), nameof(NotificationLog.Type),
            nameof(NotificationLog.Subject), nameof(NotificationLog.Body), nameof(NotificationLog.Status),
            nameof(NotificationLog.SentAt), nameof(NotificationLog.IsRead), nameof(NotificationLog.ReadAt),
            nameof(NotificationLog.Payload));

        builder.Entity<NotificationTemplate>().ApplyStandardColumnOrder(
            nameof(NotificationTemplate.Key), nameof(NotificationTemplate.Channel), nameof(NotificationTemplate.Subject),
            nameof(NotificationTemplate.Body), nameof(NotificationTemplate.Description));

        base.OnModelCreating(builder);
    }
}
