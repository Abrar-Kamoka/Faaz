using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Auth;
using Faaz.SharedKernel.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Faaz.Services.Identity.Domain.IdentityEnums;

namespace Faaz.Services.Identity.WebHost.Features.Auth.Commands;

// Soft-deletes the account in Identity (scrubs PII, blocks login, revokes sessions) per the UK GDPR
// right-to-erasure request the "Delete My Account" confirmation modal makes. This does NOT cascade
// to Booking/Payment/Review records in other services — those carry their own retention obligations
// (e.g. financial records) that GDPR itself exempts from erasure; a full cross-service erasure sweep
// is a separate, larger piece of work than this endpoint.
public class DeleteAccountCommand : IRequest
{
    public Guid UserId { get; set; }
}

internal sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRefreshTokenServices _refreshTokenServices;
    private readonly ILogger<DeleteAccountCommandHandler> _logger;

    public DeleteAccountCommandHandler(
        UserManager<ApplicationUser> userManager,
        IRefreshTokenServices refreshTokenServices,
        ILogger<DeleteAccountCommandHandler> logger)
    {
        _userManager           = userManager;
        _refreshTokenServices  = refreshTokenServices;
        _logger                = logger;
    }

    public async Task Handle(DeleteAccountCommand command, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null || user.IsDeleted)
            throw new NotFoundException("User", command.UserId);

        user.FirstName  = "Deleted";
        user.LastName   = "User";
        user.IsDeleted  = true;
        user.DeletedAt  = DateTime.UtcNow;
        user.Status     = UserStatus.Suspended;
        user.PhoneNumber = null;

        // Frees the real email/username for reuse and stops it appearing anywhere PII might leak —
        // both must go through UserManager so the Normalized* columns Identity relies on for lookups
        // stay in sync.
        var placeholder = $"deleted-{user.Id:N}@deleted.faaz.local";
        await _userManager.SetEmailAsync(user, placeholder);
        await _userManager.SetUserNameAsync(user, placeholder);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw BusinessRuleException.Error(errors, "account-delete.failed");
        }

        await _refreshTokenServices.RevokeAllForUserAsync(user.Id, null, ct);
        await _refreshTokenServices.SaveChangesAsync(ct);

        _logger.LogInformation("Account deleted for UserId: {UserId}", user.Id);
    }
}
