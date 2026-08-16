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

        builder.Property(a => a.Email).HasMaxLength(254).IsRequired();
        builder.Property(a => a.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.LastName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(a => a.CurrentRole).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Institution).HasMaxLength(200);
        builder.Property(a => a.ExpertiseArea).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.Nationality).HasMaxLength(100);
        builder.Property(a => a.CountryOfResidence).HasMaxLength(100);
        builder.Property(a => a.LinkedInProfileUrl).HasMaxLength(500);
        builder.Property(a => a.PrimaryLanguage).HasMaxLength(100);
        builder.Property(a => a.PersonalStatement).HasMaxLength(2000);
        builder.Property(a => a.ReferralSource).HasMaxLength(100);
        builder.Property(a => a.AdminNotes).HasMaxLength(2000);
        builder.Property(a => a.SetupInviteToken).HasMaxLength(88);
        builder.Property(a => a.SubmittedAt).IsRequired();
        builder.Property(a => a.Remarks).HasMaxLength(500);

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
