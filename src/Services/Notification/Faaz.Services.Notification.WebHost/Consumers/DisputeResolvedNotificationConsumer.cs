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

public class DisputeResolvedNotificationConsumer : IConsumer<DisputeResolvedEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<DisputeResolvedNotificationConsumer> _logger;

    public DisputeResolvedNotificationConsumer(
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        ILogger<DisputeResolvedNotificationConsumer> logger)
    {
        _logServices = logServices;
        _hub         = hub;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<DisputeResolvedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var (studentBody, consultantBody) = msg.Resolution switch
        {
            "favour_student" => (
                $"Your dispute for booking {msg.BookingId} was resolved in your favour. A refund of £{msg.RefundAmountGbp:F2} will be processed. Note: {msg.Note}",
                $"The dispute for booking {msg.BookingId} was resolved in the student's favour. Note: {msg.Note}"),
            "favour_consultant" => (
                $"Your dispute for booking {msg.BookingId} was resolved in favour of the consultant. Note: {msg.Note}",
                $"The dispute for booking {msg.BookingId} was resolved in your favour. Note: {msg.Note}"),
            _ => (
                $"Your dispute for booking {msg.BookingId} has been closed with no action taken. Note: {msg.Note}",
                $"The dispute for booking {msg.BookingId} has been closed with no action taken. Note: {msg.Note}")
        };

        await _hub.Clients.Group($"user-{msg.StudentUserId}").SendAsync(
            "DisputeResolved",
            new { msg.BookingId, msg.Resolution, msg.RefundAmountGbp, Type = "DisputeResolved", Message = studentBody },
            ct);
        await _hub.Clients.Group($"user-{msg.ConsultantUserId}").SendAsync(
            "DisputeResolved",
            new { msg.BookingId, msg.Resolution, msg.RefundAmountGbp, Type = "DisputeResolved", Message = consultantBody },
            ct);

        var subject = "Dispute resolved";
        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.StudentUserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(DisputeResolvedEvent),
            Subject = subject,
            Body    = studentBody,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);
        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.ConsultantUserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(DisputeResolvedEvent),
            Subject = subject,
            Body    = consultantBody,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("DisputeResolved notification sent for booking {Id} ({Resolution})", msg.BookingId, msg.Resolution);
    }
}
