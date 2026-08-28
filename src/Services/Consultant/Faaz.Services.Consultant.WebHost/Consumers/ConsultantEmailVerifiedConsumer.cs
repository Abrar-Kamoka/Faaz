using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantProfile;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.WebHost.Consumers;

public class ConsultantEmailVerifiedConsumer : IConsumer<ConsultantEmailVerifiedEvent>
{
    private readonly IConsultantProfileServices _profileServices;
    private readonly IConsultantApplicationServices _applicationServices;
    private readonly ILogger<ConsultantEmailVerifiedConsumer> _logger;

    public ConsultantEmailVerifiedConsumer(
        IConsultantProfileServices profileServices,
        IConsultantApplicationServices applicationServices,
        ILogger<ConsultantEmailVerifiedConsumer> logger)
    {
        _profileServices     = profileServices;
        _applicationServices = applicationServices;
        _logger              = logger;
    }

    public async Task Consume(ConsumeContext<ConsultantEmailVerifiedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        // Set application to SettingUpProfile (was Invited)
        var app = await _applicationServices.GetByUserIdAsync(msg.UserId, ct);
        if (app is not null && app.ApplicationStatus == ConsultantApplicationStatus.Invited)
        {
            app.ApplicationStatus = ConsultantApplicationStatus.SettingUpProfile;
            await _applicationServices.SaveChangesAsync(ct);
        }

        // Create profile stub (idempotent)
        if (await _profileServices.ExistsForUserAsync(msg.UserId, ct))
        {
            _logger.LogInformation("Profile stub already exists for {UserId} — skipping", msg.UserId);
            return;
        }

        if (app is null)
        {
            _logger.LogWarning("No application found for UserId {UserId} — cannot create profile stub", msg.UserId);
            return;
        }

        // Pre-fill from the application so the consultant doesn't re-enter data they already submitted.
        await _profileServices.AddAsync(ConsultantProfileStubBuilder.Build(msg.UserId, app), ct);

        await _profileServices.SaveChangesAsync(ct);

        _logger.LogInformation("Profile stub created for consultant {UserId}", msg.UserId);
    }
}
