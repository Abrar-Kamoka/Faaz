using Faaz.Services.Consultant.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Consultant.Infrastructure.DatabaseContext.Configurations;

internal sealed class ConsultantApplicationConfiguration : IEntityTypeConfiguration<ConsultantApplication>
{
    public void Configure(EntityTypeBuilder<ConsultantApplication> builder)
    {
        builder.ToTable("ConsultantApplications");
        builder.HasKey(a => a.Id);

        // Email and SetupInviteToken stay bounded — both carry a unique/lookup index below, and
        // SQL Server does not allow nvarchar(max) as an index key column.
        builder.Property(a => a.Email).HasMaxLength(254).IsRequired();
        builder.Property(a => a.FirstName).IsRequired();
        builder.Property(a => a.LastName).IsRequired();
        builder.Property(a => a.PhoneNumber).IsRequired();
        builder.Property(a => a.CurrentRole).IsRequired();
        builder.Property(a => a.ExpertiseArea).IsRequired();
        builder.Property(a => a.SetupInviteToken).HasMaxLength(88);
        builder.Property(a => a.SubmittedAt).IsRequired();

        builder.HasIndex(a => a.Email).IsUnique().HasDatabaseName("IX_ConsultantApplications_Email");
        builder.HasIndex(a => a.ApplicationStatus).HasDatabaseName("IX_ConsultantApplications_ApplicationStatus");
        builder.HasIndex(a => a.UserId)
               .IsUnique()
               .HasFilter("[UserId] IS NOT NULL")
               .HasDatabaseName("IX_ConsultantApplications_UserId");
        // Invite token lookup happens on every consultant account creation — must be indexed.
        builder.HasIndex(a => a.SetupInviteToken)
               .HasFilter("[SetupInviteToken] IS NOT NULL")
               .HasDatabaseName("IX_ConsultantApplications_SetupInviteToken");
    }
}
