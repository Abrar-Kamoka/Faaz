using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class SessionCompletedNotificationConsumer : IConsumer<SessionCompletedEvent>
{
    private readonly ILogger<SessionCompletedNotificationConsumer> _logger;
    public SessionCompletedNotificationConsumer(ILogger<SessionCompletedNotificationConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<SessionCompletedEvent> context)
    {
        _logger.LogInformation("SessionCompleted: bookingId={BookingId} durationSeconds={Duration}", context.Message.BookingId, context.Message.ActualDurationSeconds);
        return Task.CompletedTask;
    }
}
