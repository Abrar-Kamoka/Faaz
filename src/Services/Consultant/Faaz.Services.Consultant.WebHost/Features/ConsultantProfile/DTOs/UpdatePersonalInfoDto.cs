namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;

public class UpdatePersonalInfoDto
{
    public required string FullLegalName { get; set; }
    public required string DisplayName { get; set; }
    public string? ProfessionalPhotoUrl { get; set; }
    public required string CurrentRole { get; set; }
    public required string Institution { get; set; }
    public string? LinkedInUrl { get; set; }
    public required int YearsOfExperience { get; set; }
}
