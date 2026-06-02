using Faaz.Services.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Identity.Infrastructure.DatabaseContext.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token).HasMaxLength(88).IsRequired();
        builder.Property(t => t.JwtId).HasMaxLength(36).IsRequired();
        builder.Property(t => t.CreatedByIp).HasMaxLength(45);
        builder.Property(t => t.RevokedByIp).HasMaxLength(45);
        builder.Property(t => t.Remarks).HasMaxLength(500);
        builder.Property(t => t.ExtraField1).HasMaxLength(500);
        builder.Property(t => t.ExtraField2).HasMaxLength(500);

        builder.HasIndex(t => t.Token).IsUnique().HasDatabaseName("IX_RefreshTokens_Token");

        builder.HasIndex(t => new { t.UserId, t.IsRevoked, t.IsUsed })
               .HasDatabaseName("IX_RefreshTokens_UserId_IsRevoked_IsUsed");

        builder.HasOne(t => t.User)
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
