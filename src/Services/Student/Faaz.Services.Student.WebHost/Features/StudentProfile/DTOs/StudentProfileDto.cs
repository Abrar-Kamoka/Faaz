namespace Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;

public class StudentProfileDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public string? CountryOfCitizenship { get; set; }
    public string? CountryOfResidence { get; set; }
    public string? Ethnicity { get; set; }
    public string? FirstLanguage { get; set; }
    public string[] AdditionalLanguages { get; set; } = [];
    public string? StudyTrack { get; set; }
    public SixthFormDataDto? SixthFormData { get; set; }
    public UndergraduateDataDto? UndergraduateData { get; set; }
    public PostgraduateDataDto? PostgraduateData { get; set; }
    public string? TargetStudyLevel { get; set; }
    public Guid[] TargetSubjectIds { get; set; } = [];
    public Guid[] TargetUniversityIds { get; set; } = [];
    public Guid[] TargetProgrammeIds { get; set; } = [];
    public Guid[] HelpServiceIds { get; set; } = [];
    public string? ProfilePhotoUrl { get; set; }
    public string? Bio { get; set; }
    public int ProfileCompleteness { get; set; }
    public bool IsOnboardingComplete { get; set; }
}
