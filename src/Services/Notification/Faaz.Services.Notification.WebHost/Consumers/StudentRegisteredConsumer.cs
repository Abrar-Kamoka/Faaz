using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.CommonEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class StudentRegisteredConsumer : IConsumer<StudentRegisteredEvent>
{
    private readonly IEmailSenderService _emailSender;
    private readonly INotificationLogServices _logServices;
    private readonly ILogger<StudentRegisteredConsumer> _logger;

    public StudentRegisteredConsumer(
        IEmailSenderService emailSender,
        INotificationLogServices logServices,
        ILogger<StudentRegisteredConsumer> logger)
    {
        _emailSender = emailSender;
        _logServices = logServices;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<StudentRegisteredEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var subject = "Welcome to Faaz — verify your email";
        var body = $@"<p>Hi {msg.FirstName},</p>
            <p>Welcome to Faaz! Please verify your email address:</p>
            <p><a href=""https://localhost:3000/verify-email?token={msg.VerificationToken}"">Verify Email</a></p>
            <p>The link expires in 24 hours.</p>";

        await _emailSender.SendAsync(msg.Email, subject, body, ct);

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.UserId,
            Channel = NotificationChannel.Email,
            Type    = nameof(StudentRegisteredEvent),
            Subject = subject,
            Body    = body,
            Status  = NotificationStatus.Sent,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);

        _logger.LogInformation("Welcome + verification email sent to student {UserId}", msg.UserId);
    }
}
