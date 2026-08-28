using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.Services.Notification.Infrastructure.Services;
using Faaz.Services.Notification.WebHost.Hubs;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class SessionReminderConsumer : IConsumer<SessionReminderEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly INotificationIdentityClient _identityClient;
    private readonly IEmailSenderService _emailSender;
    private readonly ILogger<SessionReminderConsumer> _logger;

    public SessionReminderConsumer(
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        INotificationIdentityClient identityClient,
        IEmailSenderService emailSender,
        ILogger<SessionReminderConsumer> logger)
    {
        _logServices    = logServices;
        _hub            = hub;
        _identityClient = identityClient;
        _emailSender    = emailSender;
        _logger         = logger;
    }

    public async Task Consume(ConsumeContext<SessionReminderEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = $"Session reminder — {msg.ReminderType}";
        var body    = $"Your session starts {msg.SessionStartUtc:f} UTC. Reminder type: {msg.ReminderType}.";

        foreach (var userId in new[] { msg.StudentId, msg.ConsultantId })
        {
            await _hub.Clients.Group($"user-{userId}").SendAsync(
                "SessionReminder",
                new { msg.BookingId, userId, Type = "SessionReminder", Message = body },
                ct);

            await _logServices.AddAsync(new NotificationLog
            {
                UserId  = userId,
                Channel = NotificationChannel.InApp,
                Type    = nameof(SessionReminderEvent),
                Subject = subject,
                Body    = body,
                Status  = NotificationStatus.Sent,
                SentAt  = DateTime.UtcNow,
                Payload = JsonSerializer.Serialize(msg)
            }, ct);

            var contact = await _identityClient.GetUserAsync(userId, ct);
            if (contact is not null)
            {
                var emailStatus = NotificationStatus.Sent;
                try
                {
                    await _emailSender.SendAsync(
                        contact.Email, subject,
                        $"<p>Hi {contact.FirstName},</p><p>{body}</p>", ct);
                }
                catch (Exception ex)
                {
                    emailStatus = NotificationStatus.Failed;
                    _logger.LogError(ex, "SessionReminder email delivery failed for user {UserId}", userId);
                }

                await _logServices.AddAsync(new NotificationLog
                {
                    UserId  = userId,
                    Channel = NotificationChannel.Email,
                    Type    = nameof(SessionReminderEvent),
                    Subject = subject,
                    Body    = body,
                    Status  = emailStatus,
                    SentAt  = DateTime.UtcNow,
                    Payload = JsonSerializer.Serialize(msg)
                }, ct);
            }
        }

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("SessionReminder notifications sent for booking {Id}", msg.BookingId);
    }
}
