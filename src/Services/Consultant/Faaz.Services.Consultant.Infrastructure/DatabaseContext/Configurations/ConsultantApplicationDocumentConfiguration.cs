using Faaz.Services.Consultant.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Consultant.Infrastructure.DatabaseContext.Configurations;

internal sealed class ConsultantApplicationDocumentConfiguration : IEntityTypeConfiguration<ConsultantApplicationDocument>
{
    public void Configure(EntityTypeBuilder<ConsultantApplicationDocument> builder)
    {
        builder.ToTable("ConsultantApplicationDocuments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DocumentType).IsRequired();
        builder.Property(d => d.FileName).IsRequired();
        builder.Property(d => d.FilePath).IsRequired();
        builder.Property(d => d.ContentType).IsRequired();
        builder.Property(d => d.FileSizeBytes).IsRequired();
        builder.Property(d => d.UploadedAt).IsRequired();

        builder.HasOne(d => d.Application)
               .WithMany(a => a.Documents)
               .HasForeignKey(d => d.ApplicationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.ApplicationId).HasDatabaseName("IX_ConsultantApplicationDocuments_ApplicationId");
    }
}
