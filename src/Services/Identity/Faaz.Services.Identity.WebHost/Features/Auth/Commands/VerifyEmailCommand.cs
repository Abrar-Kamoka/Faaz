using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.SharedKernel.Exceptions;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Faaz.Services.Identity.Domain.IdentityEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Commands;

public class VerifyEmailCommand : IRequest
{
    public VerifyEmailDto PostModel { get; set; } = null!;
}

internal sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        UserManager<ApplicationUser> userManager,
        IPublishEndpoint publishEndpoint,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _userManager     = userManager;
        _publishEndpoint = publishEndpoint;
        _logger          = logger;
    }

    public async Task Handle(VerifyEmailCommand command, CancellationToken ct)
    {
        var token = command.PostModel.Token?.Trim();
        if (string.IsNullOrEmpty(token))
            throw BusinessRuleException.Error("The verification token is invalid or has expired.", "email-verification.invalid");

        var user = _userManager.Users
            .FirstOrDefault(u => u.EmailVerificationToken == token);

        if (user is null || user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            throw BusinessRuleException.Error("The verification token is invalid or has expired.", "email-verification.invalid");

        if (!user.IsEmailVerified)
        {
            user.IsEmailVerified              = true;
            user.EmailVerificationToken       = null;
            user.EmailVerificationTokenExpiry = null;

            if (user.Role == UserRole.Consultant)
                user.ConsultantApplicationStatus = ConsultantApplicationStatus.SettingUpProfile;
            else
                // Students have no further approval gate — email verification is the last step
                // before the account is genuinely usable, so Status should say so. Consultants stay
                // PendingEmailVerification here; their own approval flow moves Status separately.
                user.Status = UserStatus.Active;

            await _userManager.UpdateAsync(user);
        }

        if (user.Role == UserRole.Consultant)
            await _publishEndpoint.Publish(new ConsultantEmailVerifiedEvent(user.Id), ct);

        _logger.LogInformation("Email verified for UserId: {UserId}", user.Id);
    }
}
