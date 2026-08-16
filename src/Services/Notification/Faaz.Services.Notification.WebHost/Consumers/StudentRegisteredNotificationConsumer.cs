using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class StudentRegisteredNotificationConsumer : IConsumer<StudentRegisteredEvent>
{
    private readonly IEmailSenderService _emailSender;
    private readonly INotificationLogServices _logServices;
    private readonly IConfiguration _config;
    private readonly ILogger<StudentRegisteredNotificationConsumer> _logger;

    public StudentRegisteredNotificationConsumer(
        IEmailSenderService emailSender,
        INotificationLogServices logServices,
        IConfiguration config,
        ILogger<StudentRegisteredNotificationConsumer> logger)
    {
        _emailSender = emailSender;
        _logServices = logServices;
        _config      = config;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<StudentRegisteredEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var frontendBaseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:3000";
        var subject = "Welcome to Faaz — verify your email";
        var body = $@"<p>Hi {msg.FirstName},</p>
            <p>Welcome to Faaz! Please verify your email address:</p>
            <p><a href=""{frontendBaseUrl}/verify-email?token={msg.VerificationToken}"">Verify Email</a></p>
            <p>The link expires in 24 hours.</p>";

        var status = NotificationStatus.Sent;
        try
        {
            await _emailSender.SendAsync(msg.Email, subject, body, ct);
        }
        catch (Exception ex)
        {
            status = NotificationStatus.Failed;
            _logger.LogError(ex, "Welcome/verification email delivery failed for student {UserId}", msg.UserId);
        }

        await _logServices.AddAsync(new NotificationLog
        {
            UserId  = msg.UserId,
            Channel = NotificationChannel.Email,
            Type    = nameof(StudentRegisteredEvent),
            Subject = subject,
            Body    = body,
            Status  = status,
            SentAt  = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(msg)
        }, ct);

        await _logServices.SaveChangesAsync(ct);

        _logger.LogInformation("Welcome/verification email status {Status} for student {UserId}", status, msg.UserId);
    }
}
