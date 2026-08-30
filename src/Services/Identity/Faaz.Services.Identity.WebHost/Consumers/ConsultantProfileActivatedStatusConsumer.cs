using Faaz.Services.Identity.Domain.Entities;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static Faaz.Services.Identity.Domain.IdentityEnums;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Identity.WebHost.Consumers;

// Named distinctly from Notification's ConsultantProfileActivatedNotificationConsumer — MassTransit
// binds a consumer's queue off its simple type name, so two identically-named consumers in different
// services would silently compete for the same queue instead of each getting their own copy of the event.
//
// Closes the sync gap between Consultant service (source of truth for profile completeness) and
// Identity (source of truth for ConsultantApplicationStatus/Status shown in the admin panel). Before
// this consumer existed, the ONLY place that promoted a consultant from SettingUpProfile to Active was
// LoginCommandHandler's on-login check — so a consultant whose profile became complete without a
// subsequent login stayed stuck at "Setting Up Profile" forever. This mirrors that exact promotion
// logic, just event-driven instead of login-triggered.
public class ConsultantProfileActivatedStatusConsumer : IConsumer<ConsultantProfileActivatedEvent>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ConsultantProfileActivatedStatusConsumer> _logger;

    public ConsultantProfileActivatedStatusConsumer(
        UserManager<ApplicationUser> userManager,
        ILogger<ConsultantProfileActivatedStatusConsumer> logger)
    {
        _userManager = userManager;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<ConsultantProfileActivatedEvent> context)
    {
        var msg = context.Message;
        var user = await _userManager.FindByIdAsync(msg.UserId.ToString());
        if (user is null || user.IsDeleted)
            return;

        if (user.ConsultantApplicationStatus != ConsultantApplicationStatus.SettingUpProfile)
            return;

        user.ConsultantApplicationStatus = ConsultantApplicationStatus.Active;
        user.Status = UserStatus.Active;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Consultant {UserId} promoted to Active via ConsultantProfileActivatedEvent", msg.UserId);
    }
}
