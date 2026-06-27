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

public class RefundIssuedConsumer : IConsumer<RefundIssuedEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<RefundIssuedConsumer> _logger;

    public RefundIssuedConsumer(
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        ILogger<RefundIssuedConsumer> logger)
    {
        _logServices = logServices;
        _hub         = hub;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<RefundIssuedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Refund issued";
        var body    = $"A refund of £{msg.TotalRefunded:F2} has been issued for booking {msg.BookingId}. Reason: {msg.Reason}";

        await _hub.Clients.Group($"user-{msg.StudentId}").SendAsync(
            "RefundIssued",
            new { msg.BookingId, msg.StudentId, msg.TotalRefunded, Type = "RefundIssued", Message = body },
            ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.StudentId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(RefundIssuedEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("RefundIssued notification sent to student {Id}", msg.StudentId);
    }
}
