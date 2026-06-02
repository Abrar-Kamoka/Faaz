using Faaz.Services.Consultant.Domain.Entities;

namespace Faaz.Services.Consultant.Domain;

/// <summary>Single source of truth for profile completeness — used by both auto-activation and the completeness query.</summary>
public static class ProfileCompletenessChecker
{
    public static Result Evaluate(ConsultantProfile p)
    {
        var personalInfo = !string.IsNullOrWhiteSpace(p.FullLegalName)
                        && !string.IsNullOrWhiteSpace(p.DisplayName)
                        && !string.IsNullOrWhiteSpace(p.CurrentRole)
                        && !string.IsNullOrWhiteSpace(p.Institution);

        var expertise = p.StudyLevelsOffered.Length > 0
                     && p.SubjectAreas.Length > 0
                     && p.ServicesOffered.Length > 0;

        var bio = !string.IsNullOrWhiteSpace(p.WrittenBio);

        // Only non-deleted, active session types count
        var pricing = p.SessionTypes.Any(s => s.IsActive);

        // Only non-deleted weekly slots count (not blocked dates)
        var availability = p.AvailabilitySlots.Any(s => !s.IsBlockedDate && !s.IsDeleted);

        return new Result(personalInfo, expertise, bio, pricing, availability);
    }

    public readonly record struct Result(
        bool HasPersonalInfo,
        bool HasExpertise,
        bool HasBio,
        bool HasPricing,
        bool HasAvailability)
    {
        public bool IsComplete =>
            HasPersonalInfo && HasExpertise && HasBio && HasPricing && HasAvailability;

        public int Percentage
        {
            get
            {
                var steps = new[] { HasPersonalInfo, HasExpertise, HasBio, HasPricing, HasAvailability };
                return (int)Math.Round(steps.Count(s => s) * 100.0 / steps.Length);
            }
        }
    }
}
