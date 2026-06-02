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

public class RegisterStudentCommand : IRequest<Guid>
{
    public RegisterStudentDto PostModel { get; set; } = null!;
}

internal sealed class RegisterStudentCommandHandler : IRequestHandler<RegisterStudentCommand, Guid>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _email;
    private readonly IStudentServiceClient _studentClient;
    private readonly ITokenService _tokenService;
    private readonly ILogger<RegisterStudentCommandHandler> _logger;

    public RegisterStudentCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService email,
        IStudentServiceClient studentClient,
        ITokenService tokenService,
        ILogger<RegisterStudentCommandHandler> logger)
    {
        _userManager = userManager;
        _email = email;
        _studentClient = studentClient;
        _tokenService = tokenService;
        _logger = logger;
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

        await _email.SendEmailVerificationAsync(command.PostModel.Email, command.PostModel.FirstName, plaintext, ct);
        await _studentClient.CreateProfileStubAsync(user.Id, command.PostModel.Email, command.PostModel.FirstName, command.PostModel.LastName, ct);

        _logger.LogInformation("Student registered: {UserId}", user.Id);
        return user.Id;
    }
}
