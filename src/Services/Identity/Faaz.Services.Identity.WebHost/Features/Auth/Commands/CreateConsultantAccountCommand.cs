using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.Services.Identity.WebHost.HttpClients;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Faaz.Services.Identity.Domain.IdentityEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Commands;

public class CreateConsultantAccountCommand : IRequest<Guid>
{
    public CreateConsultantAccountDto PostModel { get; set; } = null!;
}

internal sealed class CreateConsultantAccountCommandHandler : IRequestHandler<CreateConsultantAccountCommand, Guid>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConsultantServiceClient _consultantClient;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<CreateConsultantAccountCommandHandler> _logger;

    public CreateConsultantAccountCommandHandler(
        UserManager<ApplicationUser> userManager,
        IConsultantServiceClient consultantClient,
        IEmailService emailService,
        ITokenService tokenService,
        ILogger<CreateConsultantAccountCommandHandler> logger)
    {
        _userManager      = userManager;
        _consultantClient = consultantClient;
        _emailService     = emailService;
        _tokenService     = tokenService;
        _logger           = logger;
    }

    public async Task<Guid> Handle(CreateConsultantAccountCommand command, CancellationToken ct)
    {
        var (email, applicationId) = await _consultantClient.ValidateInviteTokenAsync(command.PostModel.InviteToken, ct);

        if (!string.Equals(email, command.PostModel.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            throw BusinessRuleException.Error(
                "The email address does not match the one associated with this invite.",
                "invite.email-mismatch");

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
            throw new ConflictException(
                "An account with this email address already exists.",
                new { userId = existingUser.Id });

        var app = await _consultantClient.GetApplicationDetailAsync(applicationId, ct);

        var (verifyPlaintext, verifyHash) = _tokenService.GenerateOpaqueToken();

        var user = new ApplicationUser
        {
            UserName                    = email,
            Email                       = email,
            FirstName                   = app.FirstName,
            LastName                    = app.LastName,
            Role                        = UserRole.Consultant,
            Status                      = UserStatus.PendingEmailVerification,
            ConsultantApplicationStatus = ConsultantApplicationStatus.Invited,
            IsEmailVerified             = false,
            EmailVerificationToken       = verifyHash,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
        };

        var result = await _userManager.CreateAsync(user, command.PostModel.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Account creation failed: {errors}");
        }

        await _consultantClient.LinkUserToApplicationAsync(applicationId, user.Id, ct);
        await _emailService.SendEmailVerificationAsync(email, app.FirstName, verifyPlaintext, ct);

        _logger.LogInformation("Consultant account created. UserId: {UserId}", user.Id);
        return user.Id;
    }
}
