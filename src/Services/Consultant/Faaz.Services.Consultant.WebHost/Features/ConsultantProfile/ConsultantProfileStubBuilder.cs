using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile;

// Single source of truth for "build a ConsultantProfile stub from an approved ConsultantApplication".
// Used to be duplicated independently in CreateProfileStubCommand and ConsultantEmailVerifiedConsumer,
// and the two drifted apart — DisplayName never combined FirstName+LastName, and Institution plus the
// StudyLevelsOffered array were missing entirely from the consumer, which is the copy that actually
// runs for every real consultant.
internal static class ConsultantProfileStubBuilder
{
    public static Domain.Entities.ConsultantProfile Build(Guid userId, Domain.Entities.ConsultantApplication application)
    {
        var studyLevels = ParseStudyLevels(application.ExpertiseArea);
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
            StudyLevelsOffered      = studyLevels
            // Subjects/Universities/Services are deliberately left empty here — the EoI wizard's
            // free-text "Subjects: Physics, Maths | Universities: Oxford | Services: UCAS Guidance"
            // segments can no longer be turned into real catalog Guids without a name→id lookup this
            // service has no way to do synchronously at stub-build time, and guessing would be exactly
            // the "not a real, verified entity" problem this catalog exists to prevent. The consultant
            // picks the real entries themselves in the setup wizard's Expertise step.
        };
    }

    private static readonly Dictionary<string, StudyLevel> StudyLevelLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sixth Form / A-Levels"]    = StudyLevel.SixthForm,
        ["Undergraduate"]            = StudyLevel.Undergraduate,
        ["Postgraduate (MSc/PhD)"]   = StudyLevel.PostgraduateTaught,
        ["PhD"]                      = StudyLevel.PostgraduateResearch,
    };

    private static StudyLevel[] ParseStudyLevels(string? expertiseArea)
    {
        if (string.IsNullOrWhiteSpace(expertiseArea))
            return [];

        foreach (var segment in expertiseArea.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || parts[0] != "Levels") continue;

            var values = parts[1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return values.Where(StudyLevelLabels.ContainsKey).Select(v => StudyLevelLabels[v]).ToArray();
        }

        return [];
    }
}
