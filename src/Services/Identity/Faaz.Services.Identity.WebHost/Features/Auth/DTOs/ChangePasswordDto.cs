namespace Faaz.Services.Identity.WebHost.Features.Auth.DTOs;

public class ChangePasswordDto
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}
