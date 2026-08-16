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

// Student-facing "payment received" notification — the money is authorized/held on their card at
// this point, not yet captured (that happens on consultant acceptance), hence "held" not "charged".
public class PaymentAuthorizedNotificationConsumer : IConsumer<PaymentAuthorizedEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly INotificationTemplateRenderer _templates;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<PaymentAuthorizedNotificationConsumer> _logger;

    public PaymentAuthorizedNotificationConsumer(
        INotificationLogServices logServices,
        INotificationTemplateRenderer templates,
        IHubContext<NotificationHub> hub,
        ILogger<PaymentAuthorizedNotificationConsumer> logger)
    {
        _logServices = logServices;
        _templates   = templates;
        _hub         = hub;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<PaymentAuthorizedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var (subject, body) = await _templates.RenderAsync(
            nameof(PaymentAuthorizedEvent),
            new Dictionary<string, string> { ["Amount"] = msg.Amount.ToString("F2") },
            fallbackSubject: "Payment received",
            fallbackBody:    $"£{msg.Amount:F2} held for your upcoming session.",
            ct);

        await _hub.Clients.Group($"user-{msg.StudentUserId}").SendAsync(
            "PaymentAuthorized",
            new { msg.BookingId, Type = "PaymentAuthorized", Message = body },
            ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.StudentUserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(PaymentAuthorizedEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("PaymentAuthorized notification sent to student {Id}", msg.StudentUserId);
    }
}
