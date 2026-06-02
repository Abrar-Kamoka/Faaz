using static Faaz.Services.Student.Domain.StudentEnums;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;

public class UpdateGoalsDto
{
    public required StudyLevel TargetStudyLevel { get; set; }
    public string[] TargetSubjects { get; set; } = [];
    public string[] TargetUniversities { get; set; } = [];
    public required HelpType HelpTypes { get; set; }
}
