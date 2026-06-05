using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.CommonEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class ConsultantRejectedConsumer : IConsumer<ConsultantRejectedEvent>
{
    private readonly IEmailSenderService _emailSender;
    private readonly INotificationLogServices _logServices;
    private readonly ILogger<ConsultantRejectedConsumer> _logger;

    public ConsultantRejectedConsumer(
        IEmailSenderService emailSender,
        INotificationLogServices logServices,
        ILogger<ConsultantRejectedConsumer> logger)
    {
        _emailSender = emailSender;
        _logServices = logServices;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<ConsultantRejectedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Update on your Faaz application";
        var reason  = string.IsNullOrWhiteSpace(msg.Reason) ? "Please contact support for more details." : msg.Reason;
        var body = $@"<p>Dear Consultant,</p>
            <p>We regret to inform you that your Faaz consultant application has not been approved at this time.</p>
            <p><strong>Reason:</strong> {reason}</p>
            <p>If you have questions, please contact our support team.</p>";

        await _emailSender.SendAsync(msg.Email, subject, body, ct);

        if (msg.UserId != Guid.Empty)
        {
            await _logServices.AddAsync(new NotificationLog
            {
                UserId  = msg.UserId,
                Channel = NotificationChannel.Email,
                Type    = nameof(ConsultantRejectedEvent),
                Subject = subject,
                Body    = body,
                Status  = NotificationStatus.Sent,
                SentAt  = DateTime.UtcNow,
                Payload = JsonSerializer.Serialize(msg)
            }, ct);

            await _logServices.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Rejection email sent for consultant {Email}", msg.Email);
    }
}
