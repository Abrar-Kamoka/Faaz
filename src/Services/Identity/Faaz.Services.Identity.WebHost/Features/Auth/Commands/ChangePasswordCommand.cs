using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Commands;

public class ChangePasswordCommand : IRequest
{
    public Guid UserId { get; set; }
    public ChangePasswordDto PostModel { get; set; } = null!;
}

internal sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(UserManager<ApplicationUser> userManager, ILogger<ChangePasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _logger      = logger;
    }

    public async Task Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null || user.IsDeleted)
            throw new NotFoundException("User", command.UserId);

        var result = await _userManager.ChangePasswordAsync(user, command.PostModel.CurrentPassword, command.PostModel.NewPassword);
        if (!result.Succeeded)
        {
            // IdentityResult's own error descriptions already distinguish "incorrect password" from
            // "password too weak" etc. — surface them rather than a generic failure.
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw BusinessRuleException.Error(errors, "password-change.failed");
        }

        _logger.LogInformation("Password changed for UserId: {UserId}", user.Id);
    }
}
