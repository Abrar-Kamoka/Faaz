using Faaz.SharedKernel.Entities;
using static Faaz.Services.Consultant.Domain.ConsultantEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.Domain.Entities;

public class ConsultantApplication : BaseSoftDeleteModel
{
    public ConsultantApplication()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid? UserId { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public bool IsUkBased { get; set; }
    public string CurrentRole { get; set; } = null!;
    public string ExpertiseArea { get; set; } = null!;
    public int YearsOfExperience { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? CountryOfResidence { get; set; }
    public string? LinkedInProfileUrl { get; set; }
    public HighestQualification? HighestQualification { get; set; }
    public string? PrimaryLanguage { get; set; }
    public string? PersonalStatement { get; set; }
    public ConsultationMode? ConsultationMode { get; set; }

    public ConsultantApplicationStatus ApplicationStatus { get; set; } = ConsultantApplicationStatus.Submitted;
    public string? AdminNotes { get; set; }
    public string? SetupInviteToken { get; set; }
    public DateTime? SetupInviteTokenExpiry { get; set; }
    public DateTime? SetupInviteSentAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? Remarks { get; set; }

    public ConsultantProfile? Profile { get; set; }
    public ICollection<ConsultantApplicationDocument> Documents { get; set; } = [];
}
