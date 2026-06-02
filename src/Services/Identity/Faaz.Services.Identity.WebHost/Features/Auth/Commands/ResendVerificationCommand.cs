using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.SharedKernel.Exceptions;
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
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ResendVerificationCommandHandler> _logger;

    public ResendVerificationCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ITokenService tokenService,
        ILogger<ResendVerificationCommandHandler> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task Handle(ResendVerificationCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(command.PostModel.Email);
        if (user is null || user.IsEmailVerified)
            throw BusinessRuleException.Error("This Email is Already Verified.", "email-verification.invalid");
        ;

        var (plaintext, hash) = _tokenService.GenerateOpaqueToken();
        user.EmailVerificationToken = hash;
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await _userManager.UpdateAsync(user);

        await _emailService.SendEmailVerificationAsync(user.Email!, user.FirstName, plaintext, ct);
        _logger.LogInformation("Verification email resent to {Email}", command.PostModel.Email);
    }
}
