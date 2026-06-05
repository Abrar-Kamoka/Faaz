using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Consultant.WebHost.Consumers;

public class ConsultantApprovedConsumer : IConsumer<ConsultantApprovedEvent>
{
    private readonly IConsultantProfileServices _profileServices;
    private readonly ILogger<ConsultantApprovedConsumer> _logger;

    public ConsultantApprovedConsumer(
        IConsultantProfileServices profileServices,
        ILogger<ConsultantApprovedConsumer> logger)
    {
        _profileServices = profileServices;
        _logger          = logger;
    }

    public async Task Consume(ConsumeContext<ConsultantApprovedEvent> context)
    {
        var msg     = context.Message;
        var profile = await _profileServices.GetByUserIdAsync(msg.UserId, context.CancellationToken);

        if (profile is null)
        {
            _logger.LogWarning("ConsultantApprovedConsumer: no profile found for {UserId}", msg.UserId);
            return;
        }

        profile.IsActive = true;
        await _profileServices.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Profile activated for consultant {UserId}", msg.UserId);
    }
}
