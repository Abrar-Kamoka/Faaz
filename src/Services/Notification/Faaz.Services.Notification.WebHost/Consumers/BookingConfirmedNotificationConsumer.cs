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

public class BookingConfirmedNotificationConsumer : IConsumer<BookingConfirmedEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly INotificationTemplateRenderer _templates;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly INotificationIdentityClient _identityClient;
    private readonly IEmailSenderService _emailSender;
    private readonly ILogger<BookingConfirmedNotificationConsumer> _logger;

    public BookingConfirmedNotificationConsumer(
        INotificationLogServices logServices,
        INotificationTemplateRenderer templates,
        IHubContext<NotificationHub> hub,
        INotificationIdentityClient identityClient,
        IEmailSenderService emailSender,
        ILogger<BookingConfirmedNotificationConsumer> logger)
    {
        _logServices    = logServices;
        _templates      = templates;
        _hub            = hub;
        _identityClient = identityClient;
        _emailSender    = emailSender;
        _logger         = logger;
    }

    public async Task Consume(ConsumeContext<BookingConfirmedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var (subject, body) = await _templates.RenderAsync(
            nameof(BookingConfirmedEvent),
            new Dictionary<string, string> { ["SessionStartUtc"] = msg.SessionStartUtc.ToString("f") },
            fallbackSubject: "Booking confirmed",
            fallbackBody:    $"Your booking has been confirmed. Session starts {msg.SessionStartUtc:f} UTC.",
            ct);

        await _hub.Clients.Group($"user-{msg.StudentId}").SendAsync(
            "BookingConfirmed",
            new { msg.BookingId, msg.StudentId, Type = "BookingConfirmed", Message = body },
            ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.StudentId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(BookingConfirmedEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        var contact = await _identityClient.GetUserAsync(msg.StudentId, ct);
        if (contact is not null)
        {
            var emailStatus = NotificationStatus.Sent;
            try { await _emailSender.SendAsync(contact.Email, subject, $"<p>{body}</p>", ct); }
            catch (Exception ex)
            {
                emailStatus = NotificationStatus.Failed;
                _logger.LogError(ex, "BookingConfirmed email delivery failed for student {Id}", msg.StudentId);
            }

            await _logServices.AddAsync(new NotificationLog
            {
                UserId  = msg.StudentId,
                Channel = NotificationChannel.Email,
                Type    = nameof(BookingConfirmedEvent),
                Subject = subject,
                Body    = body,
                Status  = emailStatus,
                SentAt  = DateTime.UtcNow,
                Payload = JsonSerializer.Serialize(msg)
            }, ct);
        }

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("BookingConfirmed notification sent to student {Id}", msg.StudentId);
    }
}
