using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.Services.Notification.WebHost.Hubs;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class BookingConfirmedConsumer : IConsumer<BookingConfirmedEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<BookingConfirmedConsumer> _logger;

    public BookingConfirmedConsumer(
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        ILogger<BookingConfirmedConsumer> logger)
    {
        _logServices = logServices;
        _hub         = hub;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<BookingConfirmedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Booking confirmed";
        var body    = $"Your booking has been confirmed. Session starts {msg.SessionStartUtc:f} UTC.";

        await _hub.Clients.Group($"user-{msg.StudentId}").SendAsync(
            "BookingConfirmed",
            new { msg.BookingId, msg.StudentId, Type = "BookingConfirmed", Message = body },
            ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.StudentId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(BookingConfirmedEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("BookingConfirmed notification sent to student {Id}", msg.StudentId);
    }
}
