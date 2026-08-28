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

public class BookingCancelledNotificationConsumer : IConsumer<BookingCancelledEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly INotificationTemplateRenderer _templates;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly INotificationIdentityClient _identityClient;
    private readonly IEmailSenderService _emailSender;
    private readonly ILogger<BookingCancelledNotificationConsumer> _logger;

    public BookingCancelledNotificationConsumer(
        INotificationLogServices logServices,
        INotificationTemplateRenderer templates,
        IHubContext<NotificationHub> hub,
        INotificationIdentityClient identityClient,
        IEmailSenderService emailSender,
        ILogger<BookingCancelledNotificationConsumer> logger)
    {
        _logServices    = logServices;
        _templates      = templates;
        _hub            = hub;
        _identityClient = identityClient;
        _emailSender    = emailSender;
        _logger         = logger;
    }

    public async Task Consume(ConsumeContext<BookingCancelledEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var refundNote = msg.RefundRequired ? $" A refund of £{msg.RefundAmount:F2} will be processed." : "";
        var (subject, body) = await _templates.RenderAsync(
            nameof(BookingCancelledEvent),
            new Dictionary<string, string>
            {
                ["BookingId"]   = msg.BookingId.ToString(),
                ["CancelledBy"] = msg.CancelledBy,
                ["Reason"]      = msg.Reason,
                ["RefundNote"]  = refundNote
            },
            fallbackSubject: "Booking cancelled",
            fallbackBody:    $"Booking {msg.BookingId} was cancelled by {msg.CancelledBy}. Reason: {msg.Reason}.{refundNote}",
            ct);

        // Notify both participants — whichever side didn't do the cancelling needs to know their
        // session is off, and the side who did benefits from a written confirmation too.
        foreach (var userId in new[] { msg.StudentId, msg.ConsultantId })
        {
            await _hub.Clients.Group($"user-{userId}").SendAsync(
                "BookingCancelled",
                new { msg.BookingId, userId, Type = "BookingCancelled", Message = body },
                ct);

            await _logServices.AddAsync(new NotificationLog
            {
                UserId  = userId,
                Channel = NotificationChannel.InApp,
                Type    = nameof(BookingCancelledEvent),
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
                try { await _emailSender.SendAsync(contact.Email, subject, $"<p>{body}</p>", ct); }
                catch (Exception ex)
                {
                    emailStatus = NotificationStatus.Failed;
                    _logger.LogError(ex, "BookingCancelled email delivery failed for user {UserId}", userId);
                }

                await _logServices.AddAsync(new NotificationLog
                {
                    UserId  = userId,
                    Channel = NotificationChannel.Email,
                    Type    = nameof(BookingCancelledEvent),
                    Subject = subject,
                    Body    = body,
                    Status  = emailStatus,
                    SentAt  = DateTime.UtcNow,
                    Payload = JsonSerializer.Serialize(msg)
                }, ct);
            }
        }

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("BookingCancelled notifications sent for booking {Id}", msg.BookingId);
    }
}
