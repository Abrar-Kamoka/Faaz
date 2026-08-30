using Faaz.SharedKernel.Entities;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Administration.Domain.Entities;

public enum ProgrammeMode
{
    FullTime = 1,
    PartTime = 2,
    Online   = 3,
    Sandwich = 4, // includes a placement/industry year
    Blended  = 5
}

public class Programme : BaseSoftDeleteModel
{
    public Programme()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid       UniversityId  { get; set; }
    public string     Title         { get; set; } = string.Empty;
    public StudyLevel StudyLevel    { get; set; }
    public ProgrammeMode Mode       { get; set; } = ProgrammeMode.FullTime;
    public int?        DurationMonths { get; set; }
    public string?      UcasCode       { get; set; }
    public string?      EntryRequirements { get; set; }
    public decimal?     TuitionFeeDomesticGbp     { get; set; }
    public decimal?     TuitionFeeInternationalGbp { get; set; }
    public bool         IsActive { get; set; } = true;

    public string?   DataSource     { get; set; }
    public string?   SourceUrl      { get; set; }
    public DateTime? LastVerifiedAt { get; set; }

    public University University { get; set; } = null!;
    public ICollection<ProgrammeSubject> ProgrammeSubjects { get; set; } = [];
}
