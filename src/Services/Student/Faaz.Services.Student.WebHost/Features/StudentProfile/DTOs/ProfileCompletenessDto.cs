namespace Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;

public class ProfileCompletenessDto
{
    public int Percentage { get; set; }
    public string[] CompletedSteps { get; set; } = [];
    public string[] MissingSteps { get; set; } = [];
}
