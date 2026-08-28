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

public class ConsultantProfileActivatedNotificationConsumer : IConsumer<ConsultantProfileActivatedEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<ConsultantProfileActivatedNotificationConsumer> _logger;

    public ConsultantProfileActivatedNotificationConsumer(
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        ILogger<ConsultantProfileActivatedNotificationConsumer> logger)
    {
        _logServices = logServices;
        _hub         = hub;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<ConsultantProfileActivatedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Welcome to Faaz!";
        var body    = $"Welcome, {msg.DisplayName}! Your profile is live — thank you for joining Faaz, we're excited to have you helping students achieve their goals.";

        await _hub.Clients.Group($"user-{msg.UserId}").SendAsync(
            "ConsultantProfileActivated",
            new { msg.UserId, Type = "ConsultantProfileActivated", Message = body },
            ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.UserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(ConsultantProfileActivatedEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("Welcome notification sent to consultant {Id}", msg.UserId);
    }
}
