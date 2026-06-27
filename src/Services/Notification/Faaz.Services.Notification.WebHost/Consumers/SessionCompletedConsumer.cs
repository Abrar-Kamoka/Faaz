using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class SessionCompletedConsumer : IConsumer<SessionCompletedEvent>
{
    private readonly ILogger<SessionCompletedConsumer> _logger;
    public SessionCompletedConsumer(ILogger<SessionCompletedConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<SessionCompletedEvent> context)
    {
        _logger.LogInformation("SessionCompleted: bookingId={BookingId} durationSeconds={Duration}", context.Message.BookingId, context.Message.ActualDurationSeconds);
        return Task.CompletedTask;
    }
}
