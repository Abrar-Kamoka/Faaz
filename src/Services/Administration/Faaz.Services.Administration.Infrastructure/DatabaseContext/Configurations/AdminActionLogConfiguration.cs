using Faaz.Services.Administration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Administration.Infrastructure.DatabaseContext.Configurations;

internal sealed class AdminActionLogConfiguration : IEntityTypeConfiguration<AdminActionLog>
{
    public void Configure(EntityTypeBuilder<AdminActionLog> builder)
    {
        builder.ToTable("AdminActionLogs", "admin");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SrNo).ValueGeneratedNever();

        builder.Property(x => x.AdminUserId).IsRequired();
        builder.Property(x => x.Action).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.BeforeJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.AfterJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.PerformedAt).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(45);

        builder.HasIndex(x => x.AdminUserId);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.PerformedAt);
    }
}
