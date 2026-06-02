using Faaz.Services.Consultant.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Consultant.Infrastructure.DatabaseContext.Configurations;

internal sealed class ConsultantSessionTypeConfiguration : IEntityTypeConfiguration<ConsultantSessionType>
{
    public void Configure(EntityTypeBuilder<ConsultantSessionType> builder)
    {
        builder.ToTable("ConsultantSessionTypes");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.PriceGbp).HasColumnType("decimal(10,2)");
        builder.Property(s => s.Remarks).HasMaxLength(500);
        builder.Property(s => s.ExtraField1).HasMaxLength(500);
        builder.Property(s => s.ExtraField2).HasMaxLength(500);

        builder.HasIndex(s => s.ConsultantProfileId).HasDatabaseName("IX_ConsultantSessionTypes_ConsultantProfileId");
    }
}
