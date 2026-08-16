using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class SendPasswordResetEmailConsumer : IConsumer<SendPasswordResetEmailEvent>
{
    private readonly IEmailSenderService _emailSender;
    private readonly IConfiguration _config;
    private readonly ILogger<SendPasswordResetEmailConsumer> _logger;

    public SendPasswordResetEmailConsumer(
        IEmailSenderService emailSender,
        IConfiguration config,
        ILogger<SendPasswordResetEmailConsumer> logger)
    {
        _emailSender = emailSender;
        _config      = config;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<SendPasswordResetEmailEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var frontendBaseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:3000";
        var body = $@"<p>Hi {msg.FirstName},</p>
            <p><a href=""{frontendBaseUrl}/reset-password?token={msg.ResetToken}"">Reset Password</a></p>
            <p>Expires in 1 hour.</p>";

        await _emailSender.SendAsync(msg.To, "Reset your Faaz password", body, ct);

        _logger.LogInformation("Password reset email sent to {To}", msg.To);
    }
}
