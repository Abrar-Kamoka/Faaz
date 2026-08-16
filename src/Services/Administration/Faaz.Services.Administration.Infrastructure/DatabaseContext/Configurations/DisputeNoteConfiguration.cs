using Faaz.Services.Administration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Administration.Infrastructure.DatabaseContext.Configurations;

internal sealed class DisputeNoteConfiguration : IEntityTypeConfiguration<DisputeNote>
{
    public void Configure(EntityTypeBuilder<DisputeNote> builder)
    {
        builder.ToTable("DisputeNotes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SrNo).ValueGeneratedNever();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.BookingId).IsRequired();
        builder.Property(x => x.AuthorAdminId).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.BookingId);
    }
}
