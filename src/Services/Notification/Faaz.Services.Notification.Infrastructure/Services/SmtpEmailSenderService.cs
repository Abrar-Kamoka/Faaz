using Faaz.Services.Notification.Infrastructure.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
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

        var host   = _config["Smtp:Host"] ?? "localhost";
        var port   = int.Parse(_config["Smtp:Port"] ?? "25");
        var useSsl = bool.Parse(_config["Smtp:UseSsl"] ?? "false");
        var username = _config["Smtp:Username"];
        var password = _config["Smtp:Password"];

        using var client = new SmtpClient();
        // The bool overload of ConnectAsync means "SSL on connect" (implicit TLS, e.g. port 465).
        // Gmail's port 587 — and most real relays — use STARTTLS instead: a plaintext connection
        // that upgrades to TLS. Using the bool overload against port 587 fails immediately with an
        // SslStream frame-size/corrupted-frame exception, because the client expects a TLS
        // ServerHello but the server sends a plaintext SMTP banner first.
        var socketOptions = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(host, port, socketOptions, ct);
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            await client.AuthenticateAsync(username, password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Email sent to {To} — subject: {Subject}", to, subject);
    }
}
