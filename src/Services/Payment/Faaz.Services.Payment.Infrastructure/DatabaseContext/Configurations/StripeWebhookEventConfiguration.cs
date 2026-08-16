using Faaz.Services.Payment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Payment.Infrastructure.DatabaseContext.Configurations;

public class StripeWebhookEventConfiguration : IEntityTypeConfiguration<StripeWebhookEvent>
{
    public void Configure(EntityTypeBuilder<StripeWebhookEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.StripeEventId).IsUnique();
        // StripeEventId stays bounded — it carries a unique index above, and SQL Server does not
        // allow nvarchar(max) as an index key column.
        builder.Property(x => x.StripeEventId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.EventType).IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();
        // NO HasQueryFilter — webhook events are never soft-deleted; we need the full log
    }
}
