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

        base.OnModelCreating(builder);
    }
}
