using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.Services.Notification.WebHost.Hubs;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.CommonEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class ConsultantApprovedConsumer : IConsumer<ConsultantApprovedEvent>
{
    private readonly IEmailSenderService _emailSender;
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<ConsultantApprovedConsumer> _logger;

    public ConsultantApprovedConsumer(
        IEmailSenderService emailSender,
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        ILogger<ConsultantApprovedConsumer> logger)
    {
        _emailSender = emailSender;
        _logServices = logServices;
        _hub         = hub;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<ConsultantApprovedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Congratulations — your Faaz profile is approved!";
        var body = $@"<p>Hi {msg.FirstName},</p>
            <p>Your consultant profile has been approved.</p>
            <p><a href=""https://localhost:3000/consultant/dashboard"">Go to your dashboard</a></p>";

        await _emailSender.SendAsync(msg.Email, subject, body, ct);

        await _hub.Clients.Group($"user-{msg.UserId}").SendAsync(
            "ConsultantApproved",
            new { msg.UserId, Type = "ConsultantApproved", Message = "Your profile has been approved!" },
            ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.UserId,
            Channel = NotificationChannel.Email,
            Type    = nameof(ConsultantApprovedEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);

        _logger.LogInformation("Approval email + SignalR push sent for consultant {UserId}", msg.UserId);
    }
}
