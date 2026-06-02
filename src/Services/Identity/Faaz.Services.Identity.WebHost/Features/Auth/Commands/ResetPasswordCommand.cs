using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Auth;
using Faaz.Services.Identity.WebHost.Features.Auth.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Commands;

public class ResetPasswordCommand : IRequest
{
    public ResetPasswordDto PostModel { get; set; } = null!;
}

internal sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordResetTokenServices _tokenServices;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IPasswordResetTokenServices tokenServices,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenServices = tokenServices;
        _logger = logger;
    }

    public async Task Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        var tokenHash = HashToken(command.PostModel.Token);
        var record = await _tokenServices.GetByTokenHashAsync(tokenHash, ct);

        if (record is null || record.IsUsed || record.ExpiresAt < DateTime.UtcNow)
            throw BusinessRuleException.Error("The reset token is invalid or has expired.", "password-reset.invalid");

        var user = await _userManager.FindByIdAsync(record.UserId.ToString());
        if (user is null)
            throw new NotFoundException("User", record.UserId);

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, command.PostModel.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw BusinessRuleException.Error(errors, "password-reset.failed");
        }

        record.IsUsed = true;
        await _tokenServices.SaveChangesAsync(ct);

        _logger.LogInformation("Password reset for UserId: {UserId}", record.UserId);
    }

    private static string HashToken(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(bytes);
    }
}
