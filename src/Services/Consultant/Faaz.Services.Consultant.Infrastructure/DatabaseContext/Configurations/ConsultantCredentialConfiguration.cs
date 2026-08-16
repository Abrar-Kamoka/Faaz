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

        builder.Property(c => c.FileName).IsRequired();
        builder.Property(c => c.StoredPath).IsRequired();
        builder.Property(c => c.ContentType).IsRequired();

        builder.HasIndex(c => c.ConsultantProfileId)
               .HasDatabaseName("IX_ConsultantCredentials_ConsultantProfileId");

        builder.HasOne(c => c.Profile)
               .WithMany(p => p.Credentials)
               .HasForeignKey(c => c.ConsultantProfileId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("FK_ConsultantCredentials_ConsultantProfiles_ConsultantProfileId");
    }
}
