using Faaz.Services.Consultant.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Consultant.Infrastructure.DatabaseContext.Configurations;

internal sealed class ConsultantAvailabilitySlotConfiguration : IEntityTypeConfiguration<ConsultantAvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<ConsultantAvailabilitySlot> builder)
    {
        builder.ToTable("ConsultantAvailabilitySlots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DayOfWeek).HasConversion<int?>();
        builder.Property(s => s.Reason).HasMaxLength(200);
        builder.Property(s => s.Remarks).HasMaxLength(500);
        builder.Property(s => s.ExtraField1).HasMaxLength(500);
        builder.Property(s => s.ExtraField2).HasMaxLength(500);

        builder.HasIndex(s => s.ConsultantProfileId).HasDatabaseName("IX_ConsultantAvailabilitySlots_ConsultantProfileId");
    }
}
