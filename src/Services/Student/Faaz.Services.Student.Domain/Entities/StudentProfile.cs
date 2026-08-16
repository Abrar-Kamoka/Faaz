using Faaz.SharedKernel.Entities;
using static Faaz.Services.Student.Domain.StudentEnums;

namespace Faaz.Services.Student.Domain.Entities;

public class StudentProfile : BaseSoftDeleteModel
{
    public StudentProfile()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public string? CountryOfCitizenship { get; set; }
    public string? CountryOfResidence { get; set; }
    public string? Ethnicity { get; set; }
    public string? FirstLanguage { get; set; }
    public string[] AdditionalLanguages { get; set; } = [];
    public StudyTrack? StudyTrack { get; set; }
    public StudyLevel? TargetStudyLevel { get; set; }
    public string[] TargetSubjects { get; set; } = [];
    public string[] TargetUniversities { get; set; } = [];
    public HelpType HelpTypes { get; set; } = 0;
    public string? ProfilePhotoUrl { get; set; }
    public string? Bio { get; set; }
    public int ProfileCompleteness { get; set; } = 0;
    public bool IsOnboardingComplete { get; set; } = false;

    public SixthFormData? SixthFormData { get; set; }
    public UndergraduateData? UndergraduateData { get; set; }
    public PostgraduateData? PostgraduateData { get; set; }

    public void UpdateCompleteness()
    {
        var steps = new[]
        {
            !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(FirstLanguage),
            StudyTrack.HasValue,
            TargetStudyLevel.HasValue && HelpTypes != 0,
            !string.IsNullOrWhiteSpace(Bio)
        };
        ProfileCompleteness = (int)Math.Round(steps.Count(s => s) * 100.0 / steps.Length);

        IsOnboardingComplete = steps.All(s => s);
    }
}
