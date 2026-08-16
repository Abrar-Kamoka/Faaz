using Faaz.Services.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Identity.Infrastructure.DatabaseContext.Configurations;

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.HasKey(t => t.Id);

        // Token stays bounded — it carries a unique index below, and SQL Server does not allow
        // nvarchar(max) as an index key column.
        builder.Property(t => t.Token).HasMaxLength(88).IsRequired();

        builder.HasIndex(t => t.Token).IsUnique().HasDatabaseName("IX_PasswordResetTokens_Token");

        builder.HasOne(t => t.User)
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
