using Faaz.Services.Student.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Student.Infrastructure.DatabaseContext.Configurations;

internal sealed class SavedConsultantConfiguration : IEntityTypeConfiguration<SavedConsultant>
{
    public void Configure(EntityTypeBuilder<SavedConsultant> builder)
    {
        builder.ToTable("SavedConsultants");
        builder.HasKey(s => s.Id);

        builder.HasIndex(s => new { s.StudentUserId, s.ConsultantUserId })
               .IsUnique()
               .HasDatabaseName("IX_SavedConsultants_Student_Consultant");
    }
}
