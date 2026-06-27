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

public class PayoutFailedConsumer : IConsumer<PayoutFailedEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<PayoutFailedConsumer> _logger;

    public PayoutFailedConsumer(
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        ILogger<PayoutFailedConsumer> logger)
    {
        _logServices = logServices;
        _hub         = hub;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<PayoutFailedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Payout failed";
        var body    = $"Your payout of £{msg.Amount:F2} for booking {msg.BookingId} failed. Reason: {msg.FailureReason}";

        await _hub.Clients.Group($"user-{msg.ConsultantId}").SendAsync(
            "PayoutFailed",
            new { msg.BookingId, msg.ConsultantId, Type = "PayoutFailed", Message = body },
            ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.ConsultantId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(PayoutFailedEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("PayoutFailed notification sent to consultant {Id}", msg.ConsultantId);
    }
}
