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

public class BookingRequestReceivedConsumer : IConsumer<BookingRequestReceivedEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly INotificationIdentityClient _identityClient;
    private readonly IEmailSenderService _emailSender;
    private readonly ILogger<BookingRequestReceivedConsumer> _logger;

    public BookingRequestReceivedConsumer(
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        INotificationIdentityClient identityClient,
        IEmailSenderService emailSender,
        ILogger<BookingRequestReceivedConsumer> logger)
    {
        _logServices    = logServices;
        _hub            = hub;
        _identityClient = identityClient;
        _emailSender    = emailSender;
        _logger         = logger;
    }

    public async Task Consume(ConsumeContext<BookingRequestReceivedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "New booking request received";
        var body    = $"A new booking request has been received for session starting {msg.SlotStartUtc:f} UTC.";

        // Notify consultant in-app
        await _hub.Clients.Group($"user-{msg.ConsultantId}").SendAsync(
            "BookingRequestReceived",
            new { msg.BookingId, msg.ConsultantId, msg.StudentId, Type = "BookingRequestReceived", Message = body },
            ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.ConsultantId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(BookingRequestReceivedEvent),
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
                _logger.LogError(ex, "BookingRequestReceived email delivery failed for consultant {Id}", msg.ConsultantId);
            }

            await _logServices.AddAsync(new NotificationLog
            {
                UserId  = msg.ConsultantId,
                Channel = NotificationChannel.Email,
                Type    = nameof(BookingRequestReceivedEvent),
                Subject = subject,
                Body    = body,
                Status  = emailStatus,
                SentAt  = DateTime.UtcNow,
                Payload = JsonSerializer.Serialize(msg)
            }, ct);
        }

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("BookingRequestReceived notification sent to consultant {Id}", msg.ConsultantId);
    }
}
