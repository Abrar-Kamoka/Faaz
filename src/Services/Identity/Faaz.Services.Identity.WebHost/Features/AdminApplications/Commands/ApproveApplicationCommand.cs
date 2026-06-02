using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;
using Faaz.Services.Identity.WebHost.HttpClients;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Faaz.Services.Identity.Domain.IdentityEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Identity.WebHost.Features.AdminApplications.Commands;

public class ApproveApplicationCommand : IRequest
{
    public required Guid ApplicationId { get; init; }
    public AdminActionDto PostModel { get; set; } = null!;
}

internal sealed class ApproveApplicationCommandHandler : IRequestHandler<ApproveApplicationCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConsultantServiceClient _consultantClient;
    private readonly IEmailService _emailService;
    private readonly ILogger<ApproveApplicationCommandHandler> _logger;

    public ApproveApplicationCommandHandler(
        UserManager<ApplicationUser> userManager,
        IConsultantServiceClient consultantClient,
        IEmailService emailService,
        ILogger<ApproveApplicationCommandHandler> logger)
    {
        _userManager = userManager;
        _consultantClient = consultantClient;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(ApproveApplicationCommand command, CancellationToken ct)
    {
        var app = await _consultantClient.GetApplicationDetailAsync(command.ApplicationId, ct);

        // Cross-verify: admin UI auto-binds the email from the detail view (disabled field)
        if (!string.Equals(app.Email, command.PostModel.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            throw Faaz.SharedKernel.Exceptions.BusinessRuleException.Error(
                "The email address does not match the application record.",
                "admin-action.email-mismatch");

        // Consultant must have registered after the pre-approval invite before final approval.
        if (!app.UserId.HasValue)
            throw Faaz.SharedKernel.Exceptions.BusinessRuleException.Error(
                "The consultant has not yet completed account registration. Send a setup invite first (pre-approve).",
                "admin-action.registration-incomplete");

        var user = await _userManager.FindByIdAsync(app.UserId.Value.ToString())
            ?? throw new Faaz.SharedKernel.Exceptions.NotFoundException("ApplicationUser", app.UserId.Value);

        user.Status = UserStatus.Active;
        user.ConsultantApplicationStatus = ConsultantApplicationStatus.Active;
        await _userManager.UpdateAsync(user);

        await _consultantClient.ApproveApplicationAsync(command.ApplicationId, command.PostModel.Notes, ct);

        // ActivateProfileAsync throws if the profile is not yet complete — consultant must finish setup first.
        await _consultantClient.ActivateProfileAsync(app.UserId.Value, ct);

        await _emailService.SendConsultantApprovalAsync(user.Email!, user.FirstName, ct);

        _logger.LogInformation("Application {ApplicationId} approved for user {UserId}", command.ApplicationId, app.UserId);
    }
}
