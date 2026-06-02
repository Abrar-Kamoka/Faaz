using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Queries;

public record GetCurrentUserQuery(Guid UserId) : IRequest<UserDto>;

internal sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetCurrentUserQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(query.UserId.ToString());
        if (user is null)
            throw new NotFoundException("User", query.UserId);

        return new UserDto(
            Id: user.Id,
            Email: user.Email!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: user.Role.ToString(),
            Status: user.Status.ToString(),
            IsEmailVerified: user.IsEmailVerified,
            ConsultantApplicationStatus: user.ConsultantApplicationStatus?.ToString());
    }
}
