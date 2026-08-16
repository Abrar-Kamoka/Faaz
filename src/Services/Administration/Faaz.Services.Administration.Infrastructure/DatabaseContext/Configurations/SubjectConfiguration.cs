using Faaz.Services.Administration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Administration.Infrastructure.DatabaseContext.Configurations;

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects", "admin");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SrNo).ValueGeneratedNever();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.ExtraField1).HasMaxLength(500);
        builder.Property(x => x.ExtraField2).HasMaxLength(500);

        builder.HasIndex(x => x.Name).HasFilter("[IsDeleted] = 0");
    }
}
