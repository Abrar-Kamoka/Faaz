using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.Services.Notification.WebHost.Hubs;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class StudentOnboardingCompletedNotificationConsumer : IConsumer<StudentOnboardingCompletedEvent>
{
    private readonly INotificationLogServices _logServices;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<StudentOnboardingCompletedNotificationConsumer> _logger;

    public StudentOnboardingCompletedNotificationConsumer(
        INotificationLogServices logServices,
        IHubContext<NotificationHub> hub,
        ILogger<StudentOnboardingCompletedNotificationConsumer> logger)
    {
        _logServices = logServices;
        _hub         = hub;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<StudentOnboardingCompletedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Welcome to Faaz!";
        var body    = $"Welcome, {msg.FirstName}! Your profile is all set up — we're glad you're here and can't wait to help you find the right consultant.";

        await _hub.Clients.Group($"user-{msg.UserId}").SendAsync(
            "StudentOnboardingCompleted",
            new { msg.UserId, Type = "StudentOnboardingCompleted", Message = body },
            ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.UserId,
            Channel = NotificationChannel.InApp,
            Type    = nameof(StudentOnboardingCompletedEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);
        _logger.LogInformation("Welcome notification sent to student {Id}", msg.UserId);
    }
}
