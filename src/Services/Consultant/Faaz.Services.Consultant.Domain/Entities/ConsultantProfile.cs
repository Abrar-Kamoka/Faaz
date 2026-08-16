using Faaz.SharedKernel.Entities;
using static Faaz.Services.Consultant.Domain.ConsultantEnums;

namespace Faaz.Services.Consultant.Domain.Entities;

public class ConsultantProfile : BaseSoftDeleteModel
{
    public ConsultantProfile()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid UserId { get; set; }
    public Guid ApplicationId { get; set; }
    public string FullLegalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfessionalPhotoUrl { get; set; }
    public string CurrentRole { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string? LinkedInUrl { get; set; }
    public int YearsOfExperience { get; set; }
    public int[] StudyLevelsOffered { get; set; } = [];
    public string[] SubjectAreas { get; set; } = [];
    public string[] SpecialisedUniversities { get; set; } = [];
    public int[] ServicesOffered { get; set; } = [];
    public string? WrittenBio { get; set; }
    public string? IntroVideoUrl { get; set; }
    public CallPreference CallPreference { get; set; } = CallPreference.Both;
    public int MinBookingNoticeHours { get; set; } = 24;
    public int MaxAdvanceBookingDays { get; set; } = 60;
    public bool IsProfileComplete { get; set; } = false;
    public bool IsActive { get; set; } = false;
    public bool IsFeatured { get; set; } = false;
    public string? StripeAccountId { get; set; }
    // Synced reactively from Stripe's account.updated webhook (Payment service) — never queried
    // live from Stripe on this service's own request path, per Stripe's recommended integration pattern.
    public bool IsStripeDetailsSubmitted { get; set; } = false;
    public bool IsStripeChargesEnabled { get; set; } = false;

    public ConsultantApplication Application { get; set; } = null!;
    public ICollection<ConsultantSessionType> SessionTypes { get; set; } = [];
    public ICollection<ConsultantAvailabilitySlot> AvailabilitySlots { get; set; } = [];
    public ICollection<ConsultantCredential> Credentials { get; set; } = [];
}
