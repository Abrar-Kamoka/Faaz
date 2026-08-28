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

public class SessionNoShowNotificationConsumer : IConsumer<SessionNoShowEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly INotificationIdentityClient _identityClient;
    private readonly IEmailSenderService _emailSender;
    private readonly ILogger<SessionNoShowNotificationConsumer> _logger;

    public SessionNoShowNotificationConsumer(
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        INotificationIdentityClient identityClient,
        IEmailSenderService emailSender,
        ILogger<SessionNoShowNotificationConsumer> logger)
    {
        _logServices    = logServices;
        _hub            = hub;
        _identityClient = identityClient;
        _emailSender    = emailSender;
        _logger         = logger;
    }

    public async Task Consume(ConsumeContext<SessionNoShowEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        // Every recipient gets a message tailored to what THEY need to know, not a shared
        // admin-facing summary — the party who missed it needs to know they missed it, and the
        // party who showed up needs to know the other side didn't (not just "a no-show happened").
        var recipients = new[]
        {
            (UserId: msg.StudentId,    Body: BodyFor(isSelf: !msg.StudentJoined,    otherPartyMissed: !msg.ConsultantJoined, otherParty: "consultant")),
            (UserId: msg.ConsultantId, Body: BodyFor(isSelf: !msg.ConsultantJoined, otherPartyMissed: !msg.StudentJoined,    otherParty: "student")),
        };
        const string subject = "Session missed";

        foreach (var (userId, body) in recipients)
        {
            await _hub.Clients.Group($"user-{userId}").SendAsync(
                "SessionNoShow",
                new { msg.BookingId, userId, Type = "SessionNoShow", Message = body },
                ct);

            await _logServices.AddAsync(new NotificationLog
            {
                UserId  = userId,
                Channel = NotificationChannel.InApp,
                Type    = nameof(SessionNoShowEvent),
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
                try { await _emailSender.SendAsync(contact.Email, subject, $"<p>Hi {contact.FirstName},</p><p>{body}</p>", ct); }
                catch (Exception ex)
                {
                    emailStatus = NotificationStatus.Failed;
                    _logger.LogError(ex, "SessionNoShow email delivery failed for user {UserId}", userId);
                }

                await _logServices.AddAsync(new NotificationLog
                {
                    UserId  = userId,
                    Channel = NotificationChannel.Email,
                    Type    = nameof(SessionNoShowEvent),
                    Subject = subject,
                    Body    = body,
                    Status  = emailStatus,
                    SentAt  = DateTime.UtcNow,
                    Payload = JsonSerializer.Serialize(msg)
                }, ct);
            }
        }

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("SessionNoShow notifications sent for booking {Id}", msg.BookingId);
    }

    private static string BodyFor(bool isSelf, bool otherPartyMissed, string otherParty) =>
        (isSelf, otherPartyMissed) switch
        {
            (true, true)   => "Neither you nor the other party joined the session in time. It has been marked as missed.",
            (true, false)  => "You didn't join your session in time and it has been marked as missed.",
            (false, true)  => $"The {otherParty} didn't join in time, so your session was marked as missed. This was not your fault — contact support if you'd like to request a refund.",
            (false, false) => "Your session was marked as missed.",
        };
}
