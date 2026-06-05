using Faaz.Services.Consultant.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Consultant.Infrastructure.DatabaseContext.Configurations;

internal sealed class ConsultantCredentialConfiguration : IEntityTypeConfiguration<ConsultantCredential>
{
    public void Configure(EntityTypeBuilder<ConsultantCredential> builder)
    {
        builder.ToTable("ConsultantCredentials");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FileName).HasMaxLength(255).IsRequired();
        builder.Property(c => c.StoredPath).HasMaxLength(1000).IsRequired();
        builder.Property(c => c.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Remarks).HasMaxLength(500);
        builder.Property(c => c.ExtraField1).HasMaxLength(500);
        builder.Property(c => c.ExtraField2).HasMaxLength(500);

        builder.HasIndex(c => c.ConsultantProfileId)
               .HasDatabaseName("IX_ConsultantCredentials_ConsultantProfileId");

        builder.HasOne(c => c.Profile)
               .WithMany(p => p.Credentials)
               .HasForeignKey(c => c.ConsultantProfileId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("FK_ConsultantCredentials_ConsultantProfiles_ConsultantProfileId");
    }
}
