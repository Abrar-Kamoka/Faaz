using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.CommonEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class ConsultantRevisionRequestedConsumer : IConsumer<ConsultantRevisionRequestedEvent>
{
    private readonly IEmailSenderService _emailSender;
    private readonly INotificationLogServices _logServices;
    private readonly ILogger<ConsultantRevisionRequestedConsumer> _logger;

    public ConsultantRevisionRequestedConsumer(
        IEmailSenderService emailSender,
        INotificationLogServices logServices,
        ILogger<ConsultantRevisionRequestedConsumer> logger)
    {
        _emailSender = emailSender;
        _logServices = logServices;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<ConsultantRevisionRequestedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Action required: revisions requested on your Faaz profile";
        var notes   = string.IsNullOrWhiteSpace(msg.Notes) ? "Please review your profile and make the necessary updates." : msg.Notes;
        var body = $@"<p>Dear Consultant,</p>
            <p>Our team has reviewed your Faaz profile and requires some revisions before it can be approved.</p>
            <p><strong>Notes from the reviewer:</strong> {notes}</p>
            <p>Please log in to update your profile and resubmit for review.</p>";

        await _emailSender.SendAsync(msg.Email, subject, body, ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.UserId,
            Channel = NotificationChannel.Email,
            Type    = nameof(ConsultantRevisionRequestedEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);

        _logger.LogInformation("Revision request email sent for consultant {UserId}", msg.UserId);
    }
}
