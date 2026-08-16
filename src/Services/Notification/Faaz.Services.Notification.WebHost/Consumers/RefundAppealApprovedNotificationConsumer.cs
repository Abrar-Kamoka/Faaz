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

public class RefundAppealApprovedNotificationConsumer : IConsumer<RefundAppealApprovedEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<RefundAppealApprovedNotificationConsumer> _logger;

    public RefundAppealApprovedNotificationConsumer(
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        ILogger<RefundAppealApprovedNotificationConsumer> logger)
    {
        _logServices = logServices;
        _hub         = hub;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<RefundAppealApprovedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Refund appeal approved";
        var body    = $"Your refund appeal for booking {msg.BookingId} has been approved. A refund of £{msg.RefundAmountGbp:F2} will be processed.";

        await _hub.Clients.Group($"user-{msg.StudentUserId}").SendAsync(
            "RefundAppealApproved",
            new
            {
                msg.BookingId,
                msg.StudentUserId,
                msg.RefundAmountGbp,
                Type    = "RefundAppealApproved",
                Message = body
            },
            ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.StudentUserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(RefundAppealApprovedEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("RefundAppealApproved notification sent to student {Id}", msg.StudentUserId);
    }
}
