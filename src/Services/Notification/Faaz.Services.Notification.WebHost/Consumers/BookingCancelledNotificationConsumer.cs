using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class BookingCancelledNotificationConsumer : IConsumer<BookingCancelledEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly INotificationTemplateRenderer _templates;
    private readonly ILogger<BookingCancelledNotificationConsumer> _logger;

    public BookingCancelledNotificationConsumer(
        INotificationLogServices logServices,
        INotificationTemplateRenderer templates,
        ILogger<BookingCancelledNotificationConsumer> logger)
    {
        _logServices = logServices;
        _templates   = templates;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<BookingCancelledEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var refundNote = msg.RefundRequired ? $" A refund of £{msg.RefundAmount:F2} will be processed." : "";
        var (subject, body) = await _templates.RenderAsync(
            nameof(BookingCancelledEvent),
            new Dictionary<string, string>
            {
                ["BookingId"]   = msg.BookingId.ToString(),
                ["CancelledBy"] = msg.CancelledBy,
                ["Reason"]      = msg.Reason,
                ["RefundNote"]  = refundNote
            },
            fallbackSubject: "Booking cancelled",
            fallbackBody:    $"Booking {msg.BookingId} was cancelled by {msg.CancelledBy}. Reason: {msg.Reason}.{refundNote}",
            ct);

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
