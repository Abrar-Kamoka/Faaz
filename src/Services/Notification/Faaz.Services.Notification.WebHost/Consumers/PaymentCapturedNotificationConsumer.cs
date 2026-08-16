using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class PaymentCapturedNotificationConsumer : IConsumer<PaymentCapturedEvent>
{
    private readonly ILogger<PaymentCapturedNotificationConsumer> _logger;
    public PaymentCapturedNotificationConsumer(ILogger<PaymentCapturedNotificationConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<PaymentCapturedEvent> context)
    {
        _logger.LogInformation("PaymentCaptured: bookingId={BookingId} amount={Amount}", context.Message.BookingId, context.Message.Amount);
        return Task.CompletedTask;
    }
}
