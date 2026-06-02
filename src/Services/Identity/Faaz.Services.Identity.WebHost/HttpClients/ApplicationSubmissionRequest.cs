using Microsoft.AspNetCore.Http;
using static Faaz.Services.Identity.Domain.IdentityEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Identity.WebHost.HttpClients;

public sealed record ApplicationSubmissionRequest
{
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string PhoneNumber { get; init; }
    public required bool IsUkBased { get; init; }
    public required string CurrentRole { get; init; }
    public required string ExpertiseArea { get; init; }
    public required int YearsOfExperience { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Nationality { get; init; }
    public string? CountryOfResidence { get; init; }
    public string? LinkedInProfileUrl { get; init; }
    public HighestQualification? HighestQualification { get; init; }
    public string? PrimaryLanguage { get; init; }
    public string? PersonalStatement { get; init; }
    public ConsultationMode? ConsultationMode { get; init; }
    public IReadOnlyList<DocumentSubmission>? Documents { get; init; }
}

public sealed record DocumentSubmission(IFormFile File, ApplicationDocumentType DocumentType);
