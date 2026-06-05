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
        if (user is null || user.IsEmailVerified)
            throw BusinessRuleException.Error("This Email is Already Verified.", "email-verification.invalid");

        var (plaintext, hash) = _tokenService.GenerateOpaqueToken();
        user.EmailVerificationToken = hash;
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await _userManager.UpdateAsync(user);

        await _publishEndpoint.Publish(new SendVerificationEmailEvent(
            user.Email!, user.FirstName, plaintext), ct);

        _logger.LogInformation("Verification email event published for {Email}", command.PostModel.Email);
    }
}
