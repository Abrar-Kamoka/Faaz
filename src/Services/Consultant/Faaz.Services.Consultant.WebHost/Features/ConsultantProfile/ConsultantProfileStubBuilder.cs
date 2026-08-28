using static Faaz.Services.Consultant.Domain.ConsultantEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile;

// Single source of truth for "build a ConsultantProfile stub from an approved ConsultantApplication".
// Used to be duplicated independently in CreateProfileStubCommand and ConsultantEmailVerifiedConsumer,
// and the two drifted apart — DisplayName never combined FirstName+LastName, and Institution plus all
// four expertise arrays (StudyLevelsOffered/SubjectAreas/SpecialisedUniversities/ServicesOffered) were
// missing entirely from the consumer, which is the copy that actually runs for every real consultant.
internal static class ConsultantProfileStubBuilder
{
    public static Domain.Entities.ConsultantProfile Build(Guid userId, Domain.Entities.ConsultantApplication application)
    {
        var (studyLevels, subjects, universities, services) = ParseExpertiseArea(application.ExpertiseArea);
        var fullName = $"{application.FirstName} {application.LastName}".Trim();

        return new Domain.Entities.ConsultantProfile
        {
            UserId                  = userId,
            ApplicationId           = application.Id,
            FullLegalName           = fullName,
            DisplayName             = fullName,
            CurrentRole             = application.CurrentRole,
            Institution             = application.Institution ?? string.Empty,
            LinkedInUrl             = application.LinkedInProfileUrl,
            YearsOfExperience       = application.YearsOfExperience,
            StudyLevelsOffered      = studyLevels,
            SubjectAreas            = subjects,
            SpecialisedUniversities = universities,
            ServicesOffered         = services
        };
    }

    // The join-as-consultant EoI wizard has no structured columns for the study-levels/subjects/
    // universities/services the applicant picks — it flattens all four into one reporting string
    // on submit, e.g. "Levels: Undergraduate | Subjects: Physics, Maths | Universities: Oxford |
    // Services: UCAS Guidance". Unpack that same format here so the setup wizard's Expertise step
    // (which already pre-fills from these exact profile fields) shows it without asking again.
    private static readonly Dictionary<string, StudyLevel> StudyLevelLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sixth Form / A-Levels"]    = StudyLevel.ALevel,
        ["Undergraduate"]            = StudyLevel.Undergraduate,
        ["Postgraduate (MSc/PhD)"]   = StudyLevel.Postgraduate,
        ["PhD"]                      = StudyLevel.Phd,
    };

    private static readonly Dictionary<string, ServiceType> ServiceLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Personal Statement Review"] = ServiceType.PersonalStatement,
        ["UCAS Guidance"]             = ServiceType.Ucas,
        ["Interview Preparation"]     = ServiceType.InterviewPrep,
        ["SOP Review"]                = ServiceType.Sop,
        ["Scholarships"]              = ServiceType.Scholarships,
        ["Visa Guidance"]             = ServiceType.Visa,
        ["General Guidance"]          = ServiceType.GeneralGuidance,
    };

    private static (int[] StudyLevels, string[] Subjects, string[] Universities, int[] Services) ParseExpertiseArea(string? expertiseArea)
    {
        if (string.IsNullOrWhiteSpace(expertiseArea))
            return ([], [], [], []);

        var studyLevels  = new List<int>();
        var subjects     = new List<string>();
        var universities = new List<string>();
        var services     = new List<int>();

        foreach (var segment in expertiseArea.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2) continue;

            var values = parts[1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0])
            {
                case "Levels":
                    studyLevels.AddRange(values.Where(StudyLevelLabels.ContainsKey).Select(v => (int)StudyLevelLabels[v]));
                    break;
                case "Subjects":
                    subjects.AddRange(values);
                    break;
                case "Universities":
                    universities.AddRange(values);
                    break;
                case "Services":
                    services.AddRange(values.Where(ServiceLabels.ContainsKey).Select(v => (int)ServiceLabels[v]));
                    break;
            }
        }

        return (studyLevels.ToArray(), subjects.ToArray(), universities.ToArray(), services.ToArray());
    }
}
