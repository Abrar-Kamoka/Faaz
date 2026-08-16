using Microsoft.AspNetCore.Http;
using static Faaz.Services.Consultant.Domain.ConsultantEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.DTOs;

public class InitialiseApplicationDto
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public required bool IsUkBased { get; set; }
    public required string CurrentRole { get; set; }
    public string? Institution { get; set; }
    public required string ExpertiseArea { get; set; }
    public required int YearsOfExperience { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? CountryOfResidence { get; set; }
    public string? LinkedInProfileUrl { get; set; }
    public HighestQualification? HighestQualification { get; set; }
    public string? PrimaryLanguage { get; set; }
    public string? PersonalStatement { get; set; }
    public ConsultationMode? ConsultationMode { get; set; }
    public string? ReferralSource { get; set; }

    // Parallel lists — Files[i] corresponds to DocumentTypes[i].
    // DocumentTypes uses nullable enum so empty form values bind as null instead of a parse error.
    public List<IFormFile>? Files { get; set; }
    public List<ApplicationDocumentType?>? DocumentTypes { get; set; }
}
