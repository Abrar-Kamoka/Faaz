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

public class BookingRescheduledConsumer : IConsumer<BookingRescheduledEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly INotificationIdentityClient _identityClient;
    private readonly IEmailSenderService _emailSender;
    private readonly ILogger<BookingRescheduledConsumer> _logger;

    public BookingRescheduledConsumer(
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        INotificationIdentityClient identityClient,
        IEmailSenderService emailSender,
        ILogger<BookingRescheduledConsumer> logger)
    {
        _logServices    = logServices;
        _hub            = hub;
        _identityClient = identityClient;
        _emailSender    = emailSender;
        _logger         = logger;
    }

    public async Task Consume(ConsumeContext<BookingRescheduledEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Booking rescheduled — please re-confirm";
        var body    = $"The student moved this booking to {msg.NewStartUtc:f} UTC (was {msg.OldStartUtc:f} UTC). Please review and confirm the new time.";

        // Only the consultant needs to act (re-confirm) — the student already knows, they just did it.
        await _hub.Clients.Group($"user-{msg.ConsultantId}").SendAsync(
            "BookingRescheduled",
            new { msg.BookingId, msg.ConsultantId, msg.StudentId, Type = "BookingRescheduled", Message = body },
            ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.ConsultantId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(BookingRescheduledEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        var contact = await _identityClient.GetUserAsync(msg.ConsultantId, ct);
        if (contact is not null)
        {
            var emailStatus = NotificationStatus.Sent;
            try { await _emailSender.SendAsync(contact.Email, subject, $"<p>Hi {contact.FirstName},</p><p>{body}</p>", ct); }
            catch (Exception ex)
            {
                emailStatus = NotificationStatus.Failed;
                _logger.LogError(ex, "BookingRescheduled email delivery failed for consultant {Id}", msg.ConsultantId);
            }

            await _logServices.AddAsync(new NotificationLog
            {
                UserId  = msg.ConsultantId,
                Channel = NotificationChannel.Email,
                Type    = nameof(BookingRescheduledEvent),
                Subject = subject,
                Body    = body,
                Status  = emailStatus,
                SentAt  = DateTime.UtcNow,
                Payload = JsonSerializer.Serialize(msg)
            }, ct);
        }

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("BookingRescheduled notification sent to consultant {Id}", msg.ConsultantId);
    }
}
