namespace Faaz.Services.Identity.WebHost.Features.Auth.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string Status,
    bool IsEmailVerified,
    string? ConsultantApplicationStatus);
