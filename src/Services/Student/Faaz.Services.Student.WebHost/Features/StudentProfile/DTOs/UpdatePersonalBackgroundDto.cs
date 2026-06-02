namespace Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;

public class UpdatePersonalBackgroundDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? CountryOfCitizenship { get; set; }
    public string? CountryOfResidence { get; set; }
    public string? Ethnicity { get; set; }
    public required string FirstLanguage { get; set; }
    public string[] AdditionalLanguages { get; set; } = [];
}
