using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.SharedKernel.Exceptions;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Commands;

public class ResendVerificationCommand : IRequest
{
    public ResendVerificationDto PostModel { get; set; } = null!;
}

internal sealed class ResendVerificationCommandHandler : IRequestHandler<ResendVerificationCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ResendVerificationCommandHandler> _logger;

    public ResendVerificationCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IPublishEndpoint publishEndpoint,
        ILogger<ResendVerificationCommandHandler> logger)
    {
        _userManager     = userManager;
        _tokenService    = tokenService;
        _publishEndpoint = publishEndpoint;
        _logger          = logger;
    }

    public async Task Handle(ResendVerificationCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(command.PostModel.Email);
        if (user is null)
            throw BusinessRuleException.Error("No account found with that email address.", "email-verification.not-found");
        if (user.IsEmailVerified)
            throw BusinessRuleException.Error("This email address is already verified.", "email-verification.already-verified");

        // Reuse the existing token while it is still within its 24-hour window.
        // Generating a new token here would overwrite the DB value, so if the
        // original email arrives after the resend the stored token would no longer
        // match — which is exactly the mismatch bug we are fixing.
        string plaintext;
        if (!string.IsNullOrEmpty(user.EmailVerificationToken)
            && user.EmailVerificationTokenExpiry.HasValue
            && user.EmailVerificationTokenExpiry.Value > DateTime.UtcNow)
        {
            plaintext = user.EmailVerificationToken;
        }
        else
        {
            var (newPlaintext, _) = _tokenService.GenerateOpaqueToken();
            user.EmailVerificationToken       = newPlaintext;
            user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException("Failed to generate a new verification token.");
            plaintext = newPlaintext;
        }

        await _publishEndpoint.Publish(new SendVerificationEmailEvent(
            user.Email!, user.FirstName, plaintext), ct);

        _logger.LogInformation("Verification email event published for {Email}", command.PostModel.Email);
    }
}
