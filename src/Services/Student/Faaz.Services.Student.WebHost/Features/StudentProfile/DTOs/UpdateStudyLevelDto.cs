using static Faaz.Services.Student.Domain.StudentEnums;

namespace Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;

public class UpdateStudyLevelDto
{
    public required StudyTrack StudyTrack { get; set; }

    public string[]? Subjects { get; set; }
    public string? ExamBoard { get; set; }
    public string? PredictedGrades { get; set; }
    public string? School { get; set; }
    public int? TargetEntryYear { get; set; }

    public string? CurrentUniversity { get; set; }
    public bool IsGapYear { get; set; }
    public string? DegreeSubject { get; set; }
    public int? YearOfStudy { get; set; }
    public string? CurrentGrade { get; set; }

    public string? UndergraduateUniversity { get; set; }
    public string? UndergraduateDegree { get; set; }
    public string? UndergraduateGrade { get; set; }
    public string? PostgraduateStatus { get; set; }
    public string? ResearchInterests { get; set; }
}
