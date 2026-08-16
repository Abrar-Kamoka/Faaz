using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.Services.Notification.WebHost.Hubs;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class ConsultantApprovedNotificationConsumer : IConsumer<ConsultantApprovedEvent>
{
    private readonly IEmailSenderService _emailSender;
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IConfiguration _config;
    private readonly ILogger<ConsultantApprovedNotificationConsumer> _logger;

    public ConsultantApprovedNotificationConsumer(
        IEmailSenderService emailSender,
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        IConfiguration config,
        ILogger<ConsultantApprovedNotificationConsumer> logger)
    {
        _emailSender = emailSender;
        _logServices = logServices;
        _hub         = hub;
        _config      = config;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<ConsultantApprovedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        // SignalR push first — it's an in-process call to a service we already know is up. Email
        // goes through an external SMTP relay that can be slow/unreachable; if that send fails it
        // must not take the "pop" notification down with it (see catch below).
        await _hub.Clients.Group($"user-{msg.UserId}").SendAsync(
            "ConsultantApproved",
            new { msg.UserId, Type = "ConsultantApproved", Message = "Your profile has been approved!" },
            ct);

        var frontendBaseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:3000";
        var subject = "Congratulations — your Faaz profile is approved!";
        var body = $@"<p>Hi {msg.FirstName},</p>
            <p>Your consultant profile has been approved.</p>
            <p><a href=""{frontendBaseUrl}/app/consultant/dashboard"">Go to your dashboard</a></p>";

        var status = NotificationStatus.Sent;
        try
        {
            await _emailSender.SendAsync(msg.Email, subject, body, ct);
        }
        catch (Exception ex)
        {
            status = NotificationStatus.Failed;
            _logger.LogError(ex, "Approval email delivery failed for consultant {UserId}", msg.UserId);
        }

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.UserId,
            Channel = NotificationChannel.Email,
            Type    = nameof(ConsultantApprovedEvent),
            Subject = subject,
            Body    = body,
            Status  = status,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);

        _logger.LogInformation("SignalR push sent, email status {Status}, for consultant {UserId}", status, msg.UserId);
    }
}
