using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Student.Domain.Entities;

public class SixthFormData : BaseSoftDeleteModel
{
    public SixthFormData()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid StudentProfileId { get; set; }
    public string[] Subjects { get; set; } = [];
    public string? ExamBoard { get; set; }
    public string? PredictedGrades { get; set; }
    public string? School { get; set; }
    public int? TargetEntryYear { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
}
