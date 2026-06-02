using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.Services.Identity.WebHost.HttpClients;
using Faaz.SharedKernel.Exceptions;
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
    private readonly IConsultantServiceClient _consultantClient;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        UserManager<ApplicationUser> userManager,
        IConsultantServiceClient consultantClient,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _userManager = userManager;
        _consultantClient = consultantClient;
        _logger = logger;
    }

    public async Task Handle(VerifyEmailCommand command, CancellationToken ct)
    {
        var tokenHash = HashToken(command.PostModel.Token);

        var user = _userManager.Users
            .FirstOrDefault(u => u.EmailVerificationToken == tokenHash);

        if (user is null || user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            throw BusinessRuleException.Error("The verification token is invalid or has expired.", "email-verification.invalid");

        if (!user.IsEmailVerified)
        {
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;

            if (user.Role == UserRole.Consultant)
                user.ConsultantApplicationStatus = ConsultantApplicationStatus.SettingUpProfile;

            await _userManager.UpdateAsync(user);
        }

        // Always run Consultant side-effects — both are idempotent.
        // Guards against the case where UpdateAsync succeeded but a prior
        // Consultant service call failed, leaving the profile stub missing.
        if (user.Role == UserRole.Consultant)
        {
            await _consultantClient.SetApplicationUnderReviewAsync(user.Id, ct);
            await _consultantClient.CreateProfileStubAsync(user.Id, ct);
        }

        _logger.LogInformation("Email verified for UserId: {UserId}", user.Id);
    }

    private static string HashToken(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(bytes);
    }
}
