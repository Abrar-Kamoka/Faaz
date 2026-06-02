using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;
using Faaz.Services.Identity.WebHost.HttpClients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Identity.WebHost.Features.AdminApplications.Commands;

public class PreApproveApplicationCommand : IRequest
{
    public required Guid ApplicationId { get; init; }
    public AdminActionDto PostModel { get; set; } = null!;
}

internal sealed class PreApproveApplicationCommandHandler : IRequestHandler<PreApproveApplicationCommand>
{
    private readonly IConsultantServiceClient _consultantClient;
    private readonly IEmailService _emailService;
    private readonly ILogger<PreApproveApplicationCommandHandler> _logger;

    public PreApproveApplicationCommandHandler(
        IConsultantServiceClient consultantClient,
        IEmailService emailService,
        ILogger<PreApproveApplicationCommandHandler> logger)
    {
        _consultantClient = consultantClient;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(PreApproveApplicationCommand command, CancellationToken ct)
    {
        var app = await _consultantClient.GetApplicationDetailAsync(command.ApplicationId, ct);

        // Cross-verify: admin UI auto-binds the email from the detail view (disabled field)
        if (!string.Equals(app.Email, command.PostModel.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            throw Faaz.SharedKernel.Exceptions.BusinessRuleException.Error(
                "The email address does not match the application record.",
                "admin-action.email-mismatch");

        // Consultant service generates + stores the token hash; returns the plaintext for the email
        var inviteToken = await _consultantClient.PreApproveApplicationAsync(command.ApplicationId, command.PostModel.Notes, ct);

        await _emailService.SendConsultantSetupInviteAsync(app.Email, inviteToken, ct);

        _logger.LogInformation("Application {ApplicationId} pre-approved", command.ApplicationId);
    }
}
