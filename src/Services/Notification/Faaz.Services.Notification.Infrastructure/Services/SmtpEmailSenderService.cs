using Faaz.Services.Notification.Infrastructure.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Faaz.Services.Notification.Infrastructure.Services;

internal sealed class SmtpEmailSenderService : IEmailSenderService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSenderService> _logger;

    public SmtpEmailSenderService(IConfiguration config, ILogger<SmtpEmailSenderService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _config["Smtp:FromName"] ?? "Faaz Platform",
            _config["Smtp:FromEmail"] ?? "no-reply@faaz.co.uk"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body    = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _config["Smtp:Host"] ?? "localhost",
            int.Parse(_config["Smtp:Port"] ?? "25"),
            bool.Parse(_config["Smtp:UseSsl"] ?? "false"),
            ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Email sent to {To} — subject: {Subject}", to, subject);
    }
}
