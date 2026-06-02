namespace Faaz.Services.Identity.WebHost.Features.Auth.DTOs;

public class ResetPasswordDto
{
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmPassword { get; set; }
}
