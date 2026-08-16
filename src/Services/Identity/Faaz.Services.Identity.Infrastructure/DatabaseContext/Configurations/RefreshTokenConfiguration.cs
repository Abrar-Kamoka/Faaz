using Faaz.Services.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Identity.Infrastructure.DatabaseContext.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);

        // Token stays bounded — it carries a unique index below, and SQL Server does not allow
        // nvarchar(max) as an index key column.
        builder.Property(t => t.Token).HasMaxLength(88).IsRequired();
        builder.Property(t => t.JwtId).IsRequired();

        builder.HasIndex(t => t.Token).IsUnique().HasDatabaseName("IX_RefreshTokens_Token");

        builder.HasIndex(t => new { t.UserId, t.IsRevoked, t.IsUsed })
               .HasDatabaseName("IX_RefreshTokens_UserId_IsRevoked_IsUsed");

        builder.HasOne(t => t.User)
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
