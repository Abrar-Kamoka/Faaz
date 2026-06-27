using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class SessionNoShowConsumer : IConsumer<SessionNoShowEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly ILogger<SessionNoShowConsumer> _logger;

    public SessionNoShowConsumer(
        INotificationLogServices logServices,
        ILogger<SessionNoShowConsumer> logger)
    {
        _logServices = logServices;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<SessionNoShowEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var who     = (!msg.StudentJoined && !msg.ConsultantJoined) ? "both participants"
                    : !msg.StudentJoined                            ? "the student"
                    :                                                  "the consultant";
        var subject = "Session no-show detected";
        var body    = $"Session for booking {msg.BookingId} ended without {who} joining.";

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = Guid.Empty,
            Channel = NotificationChannel.InApp,
            Type    = nameof(SessionNoShowEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("SessionNoShow notification logged for booking {Id}", msg.BookingId);
    }
}
