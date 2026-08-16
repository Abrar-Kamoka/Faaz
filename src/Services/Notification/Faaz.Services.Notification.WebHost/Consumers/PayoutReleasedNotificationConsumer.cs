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

public class PayoutReleasedNotificationConsumer : IConsumer<PayoutReleasedEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<PayoutReleasedNotificationConsumer> _logger;

    public PayoutReleasedNotificationConsumer(
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        ILogger<PayoutReleasedNotificationConsumer> logger)
    {
        _logServices = logServices;
        _hub         = hub;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<PayoutReleasedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Payout released";
        var body    = $"Your payout of £{msg.NetAmount:F2} for booking {msg.BookingId} has been released.";

        await _hub.Clients.Group($"user-{msg.ConsultantId}").SendAsync(
            "PayoutReleased",
            new { msg.BookingId, msg.ConsultantId, msg.NetAmount, Type = "PayoutReleased", Message = body },
            ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.ConsultantId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(PayoutReleasedEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("PayoutReleased notification sent to consultant {Id}", msg.ConsultantId);
    }
}
