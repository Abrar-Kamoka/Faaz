using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Faaz.Services.Identity.Infrastructure.Services;

internal sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _frontendBaseUrl;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
        _frontendBaseUrl = config["FrontendBaseUrl"] ?? "http://localhost:3000";
    }

    public Task SendEmailVerificationAsync(string toEmail, string firstName, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";
        return SendAsync(
            to: toEmail,
            subject: "Verify your Faaz email address",
            body: $"<p>Hi {firstName},</p><p>Click the link below to verify your email address:</p><p><a href=\"{link}\">{link}</a></p><p>This link expires in 24 hours.</p>",
            ct: ct);
    }

    public Task SendPasswordResetAsync(string toEmail, string firstName, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        return SendAsync(
            to: toEmail,
            subject: "Reset your Faaz password",
            body: $"<p>Hi {firstName},</p><p>Click the link below to reset your password:</p><p><a href=\"{link}\">{link}</a></p><p>This link expires in 15 minutes.</p>",
            ct: ct);
    }

    public Task SendConsultantSetupInviteAsync(string toEmail, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/register/consultant/setup?token={Uri.EscapeDataString(token)}";
        return SendAsync(
            to: toEmail,
            subject: "Your Faaz consultant account invitation",
            body: $"<p>Your application to join Faaz has been reviewed. Please click the link below to create your account:</p><p><a href=\"{link}\">{link}</a></p><p>This invitation expires in 72 hours.</p>",
            ct: ct);
    }

    public Task SendConsultantApprovalAsync(string toEmail, string firstName, CancellationToken ct)
    {
        return SendAsync(
            to: toEmail,
            subject: "Your Faaz application has been approved",
            body: $"<p>Hi {firstName},</p><p>Congratulations — your Faaz consultant profile has been approved. You can now log in and start accepting bookings.</p>",
            ct: ct);
    }

    public Task SendConsultantRevisionRequestAsync(string toEmail, string firstName, string notes, CancellationToken ct)
    {
        return SendAsync(
            to: toEmail,
            subject: "Action required: Faaz application revision",
            body: $"<p>Hi {firstName},</p><p>Our team has reviewed your application and requires some changes before we can approve it:</p><blockquote>{notes}</blockquote><p>Please log in and update your profile accordingly.</p>",
            ct: ct);
    }

    public Task SendConsultantRejectionAsync(string toEmail, string firstName, string reason, CancellationToken ct)
    {
        return SendAsync(
            to: toEmail,
            subject: "Faaz application update",
            body: $"<p>Hi {firstName},</p><p>Thank you for your interest in joining Faaz. After careful review, we are unable to approve your application at this time.</p><p>Reason: {reason}</p>",
            ct: ct);
    }

    private async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        var host = _config["Smtp:Host"] ?? "localhost";
        var port = int.Parse(_config["Smtp:Port"] ?? "25");
        var useSsl = bool.Parse(_config["Smtp:UseSsl"] ?? "false");
        var fromEmail = _config["Smtp:FromEmail"] ?? "no-reply@faaz.co.uk";
        var fromName = _config["Smtp:FromName"] ?? "Faaz";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();
        try
        {
            var socketOptions = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(host, port, socketOptions, ct);
            await client.SendAsync(message, ct);
            _logger.LogInformation("Email sent to {To} with subject {Subject}", to, subject);
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}
