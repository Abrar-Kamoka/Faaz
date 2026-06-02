namespace Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;

public class AdminActionDto
{
    // Auto-bound from the application detail view in the admin UI — render as a disabled field.
    // Backend cross-verifies this against the application record before taking any action.
    public required string Email { get; set; }

    public string? Notes { get; set; }
}
