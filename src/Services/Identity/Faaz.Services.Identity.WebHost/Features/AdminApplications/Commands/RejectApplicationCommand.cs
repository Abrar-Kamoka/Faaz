using Faaz.Services.Identity.Domain.Entities;
using Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;
using Faaz.Services.Identity.WebHost.HttpClients;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Faaz.Services.Identity.Domain.IdentityEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Identity.WebHost.Features.AdminApplications.Commands;

public class RejectApplicationCommand : IRequest
{
    public required Guid ApplicationId { get; init; }
    public AdminActionDto PostModel { get; set; } = null!;
}

internal sealed class RejectApplicationCommandHandler : IRequestHandler<RejectApplicationCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConsultantServiceClient _consultantClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<RejectApplicationCommandHandler> _logger;

    public RejectApplicationCommandHandler(
        UserManager<ApplicationUser> userManager,
        IConsultantServiceClient consultantClient,
        IPublishEndpoint publishEndpoint,
        ILogger<RejectApplicationCommandHandler> logger)
    {
        _userManager     = userManager;
        _consultantClient = consultantClient;
        _publishEndpoint = publishEndpoint;
        _logger          = logger;
    }

    public async Task Handle(RejectApplicationCommand command, CancellationToken ct)
    {
        var app = await _consultantClient.GetApplicationDetailAsync(command.ApplicationId, ct);

        // Cross-verify: admin UI auto-binds the email from the detail view (disabled field)
        if (!string.Equals(app.Email, command.PostModel.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            throw Faaz.SharedKernel.Exceptions.BusinessRuleException.Error(
                "The email address does not match the application record.",
                "admin-action.email-mismatch");

        // UserId may be null if rejected before the consultant has registered
        ApplicationUser? user = null;
        if (app.UserId.HasValue)
        {
            user = await _userManager.FindByIdAsync(app.UserId.Value.ToString());
            if (user is not null)
            {
                user.Status = UserStatus.Rejected;
                user.ConsultantApplicationStatus = ConsultantApplicationStatus.Rejected;
                await _userManager.UpdateAsync(user);
            }
        }

        await _consultantClient.RejectApplicationAsync(command.ApplicationId, command.PostModel.Notes ?? string.Empty, ct);

        // Publish instead of direct email call.
        // Notification service consumer sends the rejection email.
        if (user is not null)
            await _publishEndpoint.Publish(new ConsultantRejectedEvent(
                user.Id, user.Email!, command.PostModel.Notes ?? string.Empty), ct);
        else
            await _publishEndpoint.Publish(new ConsultantRejectedEvent(
                Guid.Empty, app.Email, command.PostModel.Notes ?? string.Empty), ct);

        _logger.LogInformation("Application {ApplicationId} rejected", command.ApplicationId);
    }
}
