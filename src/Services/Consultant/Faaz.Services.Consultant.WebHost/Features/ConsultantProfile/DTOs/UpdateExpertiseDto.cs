using static Faaz.Services.Consultant.Domain.ConsultantEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;

public class UpdateExpertiseDto
{
    public required StudyLevel[] StudyLevelsOffered { get; set; }
    public required string[] SubjectAreas { get; set; }
    public required string[] SpecialisedUniversities { get; set; }
    public required ServiceType[] ServicesOffered { get; set; }
}
