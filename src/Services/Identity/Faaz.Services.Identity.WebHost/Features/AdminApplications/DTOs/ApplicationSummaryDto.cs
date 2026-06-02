namespace Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;

public sealed record ApplicationSummaryDto(
    Guid ApplicationId,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string CurrentRole,
    string ExpertiseArea,
    int YearsOfExperience,
    bool IsUkBased,
    string Status,
    DateTime SubmittedAt);
