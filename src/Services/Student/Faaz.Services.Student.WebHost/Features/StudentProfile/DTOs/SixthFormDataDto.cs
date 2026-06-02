namespace Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;

public class SixthFormDataDto
{
    public string[] Subjects { get; set; } = [];
    public string? ExamBoard { get; set; }
    public string? PredictedGrades { get; set; }
    public string? School { get; set; }
    public int? TargetEntryYear { get; set; }
}
