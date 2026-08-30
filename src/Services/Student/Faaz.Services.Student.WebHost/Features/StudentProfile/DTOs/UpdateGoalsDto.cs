using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;

public class UpdateGoalsDto
{
    public required StudyLevel TargetStudyLevel { get; set; }
    // Real, admin-curated catalog references (Administration service) — validated against it before
    // save, not free text and not a hardcoded enum.
    public Guid[] TargetSubjectIds { get; set; } = [];
    public Guid[] TargetUniversityIds { get; set; } = [];
    public Guid[] TargetProgrammeIds { get; set; } = [];
    public required Guid[] HelpServiceIds { get; set; }
}
