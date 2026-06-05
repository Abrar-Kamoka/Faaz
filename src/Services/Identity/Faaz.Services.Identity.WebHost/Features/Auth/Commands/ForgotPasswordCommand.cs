using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Auth;
using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Commands;

public class ForgotPasswordCommand : IRequest
{
    public ForgotPasswordDto PostModel { get; set; } = null!;
}

internal sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordResetTokenServices _tokenServices;
    private readonly ITokenService _tokenService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IPasswordResetTokenServices tokenServices,
        ITokenService tokenService,
        IPublishEndpoint publishEndpoint,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userManager     = userManager;
        _tokenServices   = tokenServices;
        _tokenService    = tokenService;
        _publishEndpoint = publishEndpoint;
        _logger          = logger;
    }

    public async Task Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(command.PostModel.Email);
        if (user is null) return;

        var (plaintext, hash) = _tokenService.GenerateOpaqueToken();

        var token = new PasswordResetToken
        {
            UserId    = user.Id,
            Token     = hash,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        await _tokenServices.AddAsync(token, ct);
        await _tokenServices.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new SendPasswordResetEmailEvent(
            user.Email!, user.FirstName, plaintext), ct);

        _logger.LogInformation("Password reset email event published for UserId: {UserId}", user.Id);
    }
}
