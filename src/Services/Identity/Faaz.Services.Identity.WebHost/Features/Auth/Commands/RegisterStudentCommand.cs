using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
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

public class RegisterStudentCommand : IRequest<Guid>
{
    public RegisterStudentDto PostModel { get; set; } = null!;
}

internal sealed class RegisterStudentCommandHandler : IRequestHandler<RegisterStudentCommand, Guid>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<RegisterStudentCommandHandler> _logger;

    public RegisterStudentCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IPublishEndpoint publishEndpoint,
        ILogger<RegisterStudentCommandHandler> logger)
    {
        _userManager     = userManager;
        _tokenService    = tokenService;
        _publishEndpoint = publishEndpoint;
        _logger          = logger;
    }

    public async Task<Guid> Handle(RegisterStudentCommand command, CancellationToken ct)
    {
        var existing = await _userManager.FindByEmailAsync(command.PostModel.Email);
        if (existing is not null)
            throw new ConflictException(
                "An account with this email already exists.",
                new { userId = existing.Id });

        var (plaintext, hash) = _tokenService.GenerateOpaqueToken();

        var user = new ApplicationUser
        {
            UserName = command.PostModel.Email,
            Email    = command.PostModel.Email,
            FirstName = command.PostModel.FirstName,
            LastName  = command.PostModel.LastName,
            Role     = UserRole.Student,
            Status   = UserStatus.PendingEmailVerification,
            IsEmailVerified = false,
            EmailVerificationToken       = hash,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
        };

        var result = await _userManager.CreateAsync(user, command.PostModel.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User creation failed: {errors}");
        }

        await _publishEndpoint.Publish(new StudentRegisteredEvent(
            user.Id,
            command.PostModel.Email,
            command.PostModel.FirstName,
            command.PostModel.LastName,
            plaintext), ct);

        _logger.LogInformation("Student registered: {UserId}", user.Id);
        return user.Id;
    }
}
