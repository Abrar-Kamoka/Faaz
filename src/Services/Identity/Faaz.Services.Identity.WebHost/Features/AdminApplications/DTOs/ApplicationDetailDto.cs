namespace Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;

public sealed record ApplicationDetailDto(
    Guid ApplicationId,
    Guid? UserId,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string CurrentRole,
    string? Institution,
    string ExpertiseArea,
    int YearsOfExperience,
    bool IsUkBased,
    DateOnly? DateOfBirth,
    string? Nationality,
    string? CountryOfResidence,
    string? LinkedInProfileUrl,
    string? HighestQualification,
    string? PrimaryLanguage,
    string? PersonalStatement,
    string? ConsultationMode,
    string? ReferralSource,
    string Status,
    string? AdminNotes,
    string? Remarks,
    DateTime SubmittedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ApplicationDocumentDto> Documents);

public sealed record ApplicationDocumentDto(
    Guid Id,
    string DocumentType,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAt,
    string? FileUrl);
