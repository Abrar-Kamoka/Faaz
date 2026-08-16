using Faaz.Services.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Identity.Infrastructure.DatabaseContext.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Role).IsRequired();
        builder.Property(u => u.Status).IsRequired();
        builder.Property(u => u.EmailVerificationToken).HasMaxLength(512);
        builder.Property(u => u.Remarks).HasMaxLength(500);
        builder.Property(u => u.ExtraField1).HasMaxLength(500);
        builder.Property(u => u.ExtraField2).HasMaxLength(500);

        builder.HasIndex(u => u.ConsultantApplicationStatus)
               .HasFilter("[Role] = 2")
               .HasDatabaseName("IX_Users_ConsultantApplicationStatus");
    }
}
