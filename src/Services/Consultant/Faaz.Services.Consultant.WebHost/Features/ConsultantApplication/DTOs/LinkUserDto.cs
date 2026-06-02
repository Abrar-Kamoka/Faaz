namespace Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.DTOs;

public class LinkUserDto
{
    public required Guid ApplicationId { get; set; }
    public required Guid UserId { get; set; }
}
