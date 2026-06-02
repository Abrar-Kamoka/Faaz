using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Student.Domain.Entities;

public class PostgraduateData : BaseSoftDeleteModel
{
    public PostgraduateData()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid StudentProfileId { get; set; }
    public string? UndergraduateUniversity { get; set; }
    public string? UndergraduateDegree { get; set; }
    public string? UndergraduateGrade { get; set; }
    public string? PostgraduateStatus { get; set; }
    public string? ResearchInterests { get; set; }
    public string? Remarks { get; set; }
    public string? ExtraField1 { get; set; }
    public string? ExtraField2 { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
}
