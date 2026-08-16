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

        builder.HasIndex(s => s.ConsultantProfileId).HasDatabaseName("IX_ConsultantAvailabilitySlots_ConsultantProfileId");
    }
}
