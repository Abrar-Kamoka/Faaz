using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Student.Domain.Entities;

public class UndergraduateData : BaseSoftDeleteModel
{
    public UndergraduateData()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid StudentProfileId { get; set; }
    public string? CurrentUniversity { get; set; }
    public bool IsGapYear { get; set; }
    public string? DegreeSubject { get; set; }
    public int? YearOfStudy { get; set; }
    public string? CurrentGrade { get; set; }
    public string? Remarks { get; set; }
    public string? ExtraField1 { get; set; }
    public string? ExtraField2 { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
}
