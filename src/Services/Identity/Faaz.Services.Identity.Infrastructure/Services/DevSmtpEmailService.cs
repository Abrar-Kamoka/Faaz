using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Faaz.Services.Identity.Infrastructure.Services;

// Development only — always logs token to console, AND tries MailHog (localhost:1025) if it's running.
internal sealed class DevSmtpEmailService : IEmailService
{
    private readonly ILogger<DevSmtpEmailService> _logger;
    private readonly string _frontendBaseUrl;

    public DevSmtpEmailService(IConfiguration config, ILogger<DevSmtpEmailService> logger)
    {
        _logger = logger;
        _frontendBaseUrl = config["FrontendBaseUrl"] ?? "http://localhost:3000";
    }

    public Task SendEmailVerificationAsync(string toEmail, string firstName, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";
        _logger.LogWarning("[DEV EMAIL] Verification link for {Email}: {Link}", toEmail, link);
        return TrySendAsync(toEmail, "Verify your Faaz email address",
            $"<p>Hi {firstName},</p><p><a href='{link}'>Verify your email</a></p><p>Token: <code>{token}</code></p>", ct);
    }

    public Task SendPasswordResetAsync(string toEmail, string firstName, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        _logger.LogWarning("[DEV EMAIL] Password reset link for {Email}: {Link}", toEmail, link);
        return TrySendAsync(toEmail, "Reset your Faaz password",
            $"<p>Hi {firstName},</p><p><a href='{link}'>Reset password</a></p><p>Token: <code>{token}</code></p>", ct);
    }

    public Task SendConsultantSetupInviteAsync(string toEmail, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/register/consultant/setup?token={Uri.EscapeDataString(token)}";
        _logger.LogWarning("[DEV EMAIL] Consultant setup invite for {Email}: {Link}", toEmail, link);
        return TrySendAsync(toEmail, "Your Faaz consultant account invitation",
            $"<p><a href='{link}'>Set up your account</a></p><p>Token: <code>{token}</code></p>", ct);
    }

    public Task SendConsultantApprovalAsync(string toEmail, string firstName, CancellationToken ct)
    {
        _logger.LogWarning("[DEV EMAIL] Consultant approved: {Email}", toEmail);
        return TrySendAsync(toEmail, "Your Faaz application has been approved",
            $"<p>Hi {firstName}, your consultant application has been approved!</p>", ct);
    }

    public Task SendConsultantRevisionRequestAsync(string toEmail, string firstName, string notes, CancellationToken ct)
    {
        _logger.LogWarning("[DEV EMAIL] Consultant revision requested for {Email}: {Notes}", toEmail, notes);
        return TrySendAsync(toEmail, "Action required: Faaz application revision",
            $"<p>Hi {firstName},</p><blockquote>{notes}</blockquote>", ct);
    }

    public Task SendConsultantRejectionAsync(string toEmail, string firstName, string reason, CancellationToken ct)
    {
        _logger.LogWarning("[DEV EMAIL] Consultant rejected {Email}: {Reason}", toEmail, reason);
        return TrySendAsync(toEmail, "Faaz application update",
            $"<p>Hi {firstName},</p><p>Reason: {reason}</p>", ct);
    }

    private async Task TrySendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Faaz Platform [Dev]", "no-reply@faaz.co.uk"));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync("localhost", 1025, SecureSocketOptions.None, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("[DEV EMAIL] Sent to MailHog → open http://localhost:8025");
        }
        catch (Exception ex)
        {
            _logger.LogDebug("[DEV EMAIL] MailHog unavailable ({Message}) — see token logged above", ex.Message);
        }
    }
}
