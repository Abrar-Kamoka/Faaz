using Faaz.SharedKernel.Entities;
using static Faaz.Services.Consultant.Domain.ConsultantEnums;
using static Faaz.SharedKernel.SharedEnums;

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
    // A 5-value enum has no scalability problem the way unbounded-cardinality subject/university/
    // service references do — stays a plain JSON int[] column rather than a join table.
    public StudyLevel[] StudyLevelsOffered { get; set; } = [];
    public string? WrittenBio { get; set; }
    public string? IntroVideoUrl { get; set; }
    public CallPreference CallPreference { get; set; } = CallPreference.Both;
    public int MinBookingNoticeHours { get; set; } = 24;
    public int MaxAdvanceBookingDays { get; set; } = 60;
    // IANA identifier (e.g. "Europe/London", "Asia/Karachi") — the timezone AvailabilitySlot's
    // StartTimeLocal/EndTimeLocal are relative to. Defaults to "UTC" so existing profiles that
    // haven't re-saved their availability since this field was added keep their exact prior
    // (previously mislabeled-as-UTC) behavior rather than silently shifting.
    public string TimeZoneId { get; set; } = "UTC";
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

    // Replaces the old free-text SubjectAreas/SpecialisedUniversities and the ServiceType-enum
    // ServicesOffered — all three now reference the real, admin-curated catalog owned by the
    // Administration service (validated via AdministrationReferenceClient before save).
    public ICollection<ConsultantProfileService> Services { get; set; } = [];
    public ICollection<ConsultantProfileSubject> Subjects { get; set; } = [];
    public ICollection<ConsultantProfileUniversity> Universities { get; set; } = [];
}
