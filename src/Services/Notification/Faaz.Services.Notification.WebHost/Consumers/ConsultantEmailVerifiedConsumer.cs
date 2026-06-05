using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;
using static Faaz.Services.Notification.Domain.CommonEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class ConsultantEmailVerifiedConsumer : IConsumer<ConsultantEmailVerifiedEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly ILogger<ConsultantEmailVerifiedConsumer> _logger;

    public ConsultantEmailVerifiedConsumer(
        INotificationLogServices logServices,
        ILogger<ConsultantEmailVerifiedConsumer> logger)
    {
        _logServices = logServices;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<ConsultantEmailVerifiedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.UserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(ConsultantEmailVerifiedEvent),
            Subject = "Application received",
            Body    = "Your email has been verified. Your application is now under review.",
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow
        }, ct);

        await _logServices.SaveChangesAsync(ct);

        _logger.LogInformation("In-app notification created for consultant email verification {UserId}", msg.UserId);
    }
}
