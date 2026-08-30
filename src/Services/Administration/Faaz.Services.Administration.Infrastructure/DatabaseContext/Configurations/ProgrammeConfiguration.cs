using Faaz.Services.Administration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faaz.Services.Administration.Infrastructure.DatabaseContext.Configurations;

internal sealed class ProgrammeConfiguration : IEntityTypeConfiguration<Programme>
{
    public void Configure(EntityTypeBuilder<Programme> builder)
    {
        builder.ToTable("Programmes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SrNo).ValueGeneratedNever();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.UcasCode).HasMaxLength(20);
        builder.Property(x => x.DataSource).HasMaxLength(200);
        builder.Property(x => x.SourceUrl).HasMaxLength(500);
        builder.Property(x => x.TuitionFeeDomesticGbp).HasColumnType("decimal(10,2)");
        builder.Property(x => x.TuitionFeeInternationalGbp).HasColumnType("decimal(10,2)");

        builder.HasOne(x => x.University)
               .WithMany()
               .HasForeignKey(x => x.UniversityId)
               .OnDelete(DeleteBehavior.Restrict);

        // Backs the University -> level -> programme cascading wizard lookup.
        builder.HasIndex(x => new { x.UniversityId, x.StudyLevel }).HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.Title).HasFilter("[IsDeleted] = 0");
    }
}

internal sealed class ProgrammeSubjectConfiguration : IEntityTypeConfiguration<ProgrammeSubject>
{
    public void Configure(EntityTypeBuilder<ProgrammeSubject> builder)
    {
        builder.ToTable("ProgrammeSubjects");
        builder.HasKey(x => new { x.ProgrammeId, x.SubjectId });

        builder.HasOne(x => x.Programme)
               .WithMany(p => p.ProgrammeSubjects)
               .HasForeignKey(x => x.ProgrammeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Subject)
               .WithMany()
               .HasForeignKey(x => x.SubjectId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
