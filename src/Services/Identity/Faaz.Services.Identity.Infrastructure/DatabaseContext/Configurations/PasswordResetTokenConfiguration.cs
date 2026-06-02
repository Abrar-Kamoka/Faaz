using Faaz.Services.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Identity.Infrastructure.DatabaseContext.Configurations;

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token).HasMaxLength(88).IsRequired();
        builder.Property(t => t.Remarks).HasMaxLength(500);
        builder.Property(t => t.ExtraField1).HasMaxLength(500);
        builder.Property(t => t.ExtraField2).HasMaxLength(500);

        builder.HasIndex(t => t.Token).IsUnique().HasDatabaseName("IX_PasswordResetTokens_Token");

        builder.HasOne(t => t.User)
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
