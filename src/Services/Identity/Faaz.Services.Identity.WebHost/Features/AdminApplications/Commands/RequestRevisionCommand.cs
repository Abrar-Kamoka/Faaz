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

public class RequestRevisionCommand : IRequest
{
    public required Guid ApplicationId { get; init; }
    public AdminActionDto PostModel { get; set; } = null!;
}

internal sealed class RequestRevisionCommandHandler : IRequestHandler<RequestRevisionCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConsultantServiceClient _consultantClient;
    private readonly IEmailService _emailService;
    private readonly ILogger<RequestRevisionCommandHandler> _logger;

    public RequestRevisionCommandHandler(
        UserManager<ApplicationUser> userManager,
        IConsultantServiceClient consultantClient,
        IEmailService emailService,
        ILogger<RequestRevisionCommandHandler> logger)
    {
        _userManager = userManager;
        _consultantClient = consultantClient;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(RequestRevisionCommand command, CancellationToken ct)
    {
        var app = await _consultantClient.GetApplicationDetailAsync(command.ApplicationId, ct);

        // Cross-verify: admin UI auto-binds the email from the detail view (disabled field)
        if (!string.Equals(app.Email, command.PostModel.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            throw Faaz.SharedKernel.Exceptions.BusinessRuleException.Error(
                "The email address does not match the application record.",
                "admin-action.email-mismatch");

        // RequestRevision is only meaningful after the consultant has registered (UserId set)
        ApplicationUser? user = null;
        if (app.UserId.HasValue)
        {
            user = await _userManager.FindByIdAsync(app.UserId.Value.ToString());
            if (user is not null)
            {
                user.ConsultantApplicationStatus = ConsultantApplicationStatus.PendingRevision;
                await _userManager.UpdateAsync(user);
            }
        }

        await _consultantClient.RequestRevisionAsync(command.ApplicationId, command.PostModel.Notes ?? string.Empty, ct);
        await _emailService.SendConsultantRevisionRequestAsync(app.Email, user?.FirstName ?? "Consultant", command.PostModel.Notes ?? string.Empty, ct);

        _logger.LogInformation("Revision requested for application {ApplicationId}", command.ApplicationId);
    }
}
