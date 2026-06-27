using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class BookingCancelledConsumer : IConsumer<BookingCancelledEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly ILogger<BookingCancelledConsumer> _logger;

    public BookingCancelledConsumer(
        INotificationLogServices logServices,
        ILogger<BookingCancelledConsumer> logger)
    {
        _logServices = logServices;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<BookingCancelledEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Booking cancelled";
        var refundNote = msg.RefundRequired ? $" A refund of £{msg.RefundAmount:F2} will be processed." : "";
        var body    = $"Booking {msg.BookingId} was cancelled by {msg.CancelledBy}. Reason: {msg.Reason}.{refundNote}";

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = Guid.Empty,
            Channel = NotificationChannel.InApp,
            Type    = nameof(BookingCancelledEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("BookingCancelled notification logged for booking {Id}", msg.BookingId);
    }
}
