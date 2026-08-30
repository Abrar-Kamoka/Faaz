using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;

public class UpdateExpertiseDto
{
    public required StudyLevel[] StudyLevelsOffered { get; set; }
    // Real, admin-curated catalog references (Administration service) — validated against it before
    // save, not free text and not a hardcoded enum.
    public required Guid[] SubjectIds { get; set; }
    public required Guid[] UniversityIds { get; set; }
    public required Guid[] ServiceIds { get; set; }
}
