namespace Faaz.Services.Identity.WebHost.Features.Auth.DTOs;

public class CreateConsultantAccountDto
{
    public required string InviteToken { get; set; }
    public required string Password { get; set; }

    // Cross-verification: must match the email on the ConsultantApplication.
    // On the frontend, pre-fill this from the invite link / the email the applicant
    // registered with, and render it as a disabled (read-only) field — the consultant
    // should see it but not be able to change it.
    public required string Email { get; set; }
}
