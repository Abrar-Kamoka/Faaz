using Faaz.Services.Administration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Administration.Infrastructure.DatabaseContext.Configurations;

internal sealed class ReferenceDataRequestConfiguration : IEntityTypeConfiguration<ReferenceDataRequest>
{
    public void Configure(EntityTypeBuilder<ReferenceDataRequest> builder)
    {
        builder.ToTable("ReferenceDataRequests");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SrNo).ValueGeneratedNever();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.RequestedByRole).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProposedName).HasMaxLength(300).IsRequired();

        builder.HasIndex(x => x.Status).HasFilter("[IsDeleted] = 0");
    }
}
