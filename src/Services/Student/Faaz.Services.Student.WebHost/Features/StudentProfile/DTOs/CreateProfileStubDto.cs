namespace Faaz.Services.Student.WebHost.Features.StudentProfile.DTOs;

public class CreateProfileStubDto
{
    public required Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}
