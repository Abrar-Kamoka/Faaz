using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class PaymentCapturedConsumer : IConsumer<PaymentCapturedEvent>
{
    private readonly ILogger<PaymentCapturedConsumer> _logger;
    public PaymentCapturedConsumer(ILogger<PaymentCapturedConsumer> logger) => _logger = logger;
    public Task Consume(ConsumeContext<PaymentCapturedEvent> context)
    {
        _logger.LogInformation("PaymentCaptured: bookingId={BookingId} amount={Amount}", context.Message.BookingId, context.Message.Amount);
        return Task.CompletedTask;
    }
}
