using Faaz.Services.Notification.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Notification.WebHost.Consumers;

public class SendVerificationEmailConsumer : IConsumer<SendVerificationEmailEvent>
{
    private readonly IEmailSenderService _emailSender;
    private readonly IConfiguration _config;
    private readonly ILogger<SendVerificationEmailConsumer> _logger;

    public SendVerificationEmailConsumer(
        IEmailSenderService emailSender,
        IConfiguration config,
        ILogger<SendVerificationEmailConsumer> logger)
    {
        _emailSender = emailSender;
        _config      = config;
        _logger      = logger;
    }

    public async Task Consume(ConsumeContext<SendVerificationEmailEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var frontendBaseUrl = _config["FrontendBaseUrl"] ?? "http://localhost:3000";
        var body = $@"<p>Hi {msg.FirstName},</p>
            <p><a href=""{frontendBaseUrl}/verify-email?token={msg.VerificationToken}"">Verify Email</a></p>
            <p>Expires in 24 hours.</p>";

        await _emailSender.SendAsync(msg.To, "Verify your Faaz email", body, ct);

        _logger.LogInformation("Verification email sent to {To}", msg.To);
    }
}
