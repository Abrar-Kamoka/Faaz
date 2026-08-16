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
        // Always log the link so developers can verify manually if SMTP is unavailable.
        _logger.LogWarning("[EMAIL VERIFY] {Email} → {Link}", toEmail, link);
        return SendAsync(
            to: toEmail,
            subject: "Verify your Faaz email address",
            body: BuildHtml(
                firstName: firstName,
                heading: "Verify your email address",
                bodyHtml: "<p style=\"margin:0 0 16px;\">Thanks for joining Faaz! Please verify your email address to activate your account.</p><p style=\"margin:0 0 24px;\">This link expires in <strong>24 hours</strong>.</p>",
                ctaUrl: link,
                ctaLabel: "Verify Email Address"),
            ct: ct);
    }

    public Task SendPasswordResetAsync(string toEmail, string firstName, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        return SendAsync(
            to: toEmail,
            subject: "Reset your Faaz password",
            body: BuildHtml(
                firstName: firstName,
                heading: "Reset your password",
                bodyHtml: "<p style=\"margin:0 0 16px;\">We received a request to reset the password for your Faaz account. Click the button below to choose a new password.</p><p style=\"margin:0 0 24px;\">If you didn't request this, you can safely ignore this email — your password won't change. This link expires in <strong>15 minutes</strong>.</p>",
                ctaUrl: link,
                ctaLabel: "Reset Password"),
            ct: ct);
    }

    public Task SendConsultantSetupInviteAsync(string toEmail, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/register/consultant/setup?token={Uri.EscapeDataString(token)}";
        return SendAsync(
            to: toEmail,
            subject: "You're invited to join Faaz as a consultant",
            body: BuildHtml(
                firstName: null,
                heading: "You've been invited to join Faaz",
                bodyHtml: "<p style=\"margin:0 0 16px;\">Your application to join Faaz as a consultant has been reviewed and we'd love to have you on board.</p><p style=\"margin:0 0 24px;\">Click the button below to set up your account and choose your password. This invitation expires in <strong>72 hours</strong>.</p>",
                ctaUrl: link,
                ctaLabel: "Set Up My Account"),
            ct: ct);
    }

    public Task SendConsultantApprovalAsync(string toEmail, string firstName, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/app/consultant/dashboard";
        return SendAsync(
            to: toEmail,
            subject: "Your Faaz profile has been approved 🎉",
            body: BuildHtml(
                firstName: firstName,
                heading: "Congratulations — you're approved!",
                bodyHtml: "<p style=\"margin:0 0 16px;\">Your Faaz consultant profile has been reviewed and approved. You can now log in, complete your setup, and start accepting bookings from students.</p><p style=\"margin:0 0 24px;\">Welcome to the Faaz consultant community!</p>",
                ctaUrl: link,
                ctaLabel: "Go to My Dashboard"),
            ct: ct);
    }

    public Task SendConsultantRevisionRequestAsync(string toEmail, string firstName, string notes, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/app/consultant/setup/personal";
        return SendAsync(
            to: toEmail,
            subject: "Action required: your Faaz application needs updates",
            body: BuildHtml(
                firstName: firstName,
                heading: "Your application needs a few changes",
                bodyHtml: $"<p style=\"margin:0 0 16px;\">Our team has reviewed your application and would like you to make the following changes before we can approve it:</p><div style=\"margin:0 0 24px;padding:16px;background:#f8f6f2;border-left:4px solid #1A9E5C;border-radius:4px;\"><p style=\"margin:0;font-size:14px;color:#555555;\">{System.Net.WebUtility.HtmlEncode(notes)}</p></div><p style=\"margin:0 0 24px;\">Please log in and update your profile to address the feedback above.</p>",
                ctaUrl: link,
                ctaLabel: "Update My Profile"),
            ct: ct);
    }

    public Task SendConsultantRejectionAsync(string toEmail, string firstName, string reason, CancellationToken ct)
    {
        return SendAsync(
            to: toEmail,
            subject: "An update on your Faaz application",
            body: BuildHtml(
                firstName: firstName,
                heading: "Thank you for applying to Faaz",
                bodyHtml: $"<p style=\"margin:0 0 16px;\">Thank you for your interest in joining Faaz as a consultant. After careful review, we are unable to approve your application at this time.</p><div style=\"margin:0 0 24px;padding:16px;background:#f8f6f2;border-left:4px solid #EF4444;border-radius:4px;\"><p style=\"margin:0;font-size:14px;color:#555555;\">{System.Net.WebUtility.HtmlEncode(reason)}</p></div><p style=\"margin:0 0 24px;\">If you have questions, please don't hesitate to reach out to our support team at <a href=\"mailto:support@faaz.co.uk\" style=\"color:#1A9E5C;\">support@faaz.co.uk</a>.</p>",
                ctaUrl: null,
                ctaLabel: null),
            ct: ct);
    }

    private string BuildHtml(string? firstName, string heading, string bodyHtml, string? ctaUrl, string? ctaLabel)
    {
        var greeting = firstName is not null ? $"Hi {System.Net.WebUtility.HtmlEncode(firstName)}," : "Hello,";
        var ctaBlock = ctaUrl is not null && ctaLabel is not null
            ? $"""
              <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 auto 32px;">
                <tr>
                  <td style="border-radius:8px;background:#1A9E5C;">
                    <a href="{ctaUrl}" target="_blank"
                       style="display:inline-block;padding:14px 28px;font-family:Inter,Arial,sans-serif;
                              font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;
                              border-radius:8px;line-height:1;">{ctaLabel}</a>
                  </td>
                </tr>
              </table>
              <p style="margin:0 0 24px;font-size:13px;color:#A0A09C;">
                Or copy this link into your browser:<br/>
                <a href="{ctaUrl}" style="color:#1A9E5C;word-break:break-all;">{ctaUrl}</a>
              </p>
              """
            : "";

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="UTF-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/></head>
            <body style="margin:0;padding:0;background:#FDFAF4;font-family:Inter,Arial,sans-serif;">
              <table role="presentation" cellpadding="0" cellspacing="0" width="100%"
                     style="background:#FDFAF4;padding:40px 16px;">
                <tr><td align="center">
                  <table role="presentation" cellpadding="0" cellspacing="0" width="600"
                         style="max-width:600px;background:#ffffff;border-radius:12px;
                                border:1px solid #E0DBD0;overflow:hidden;">

                    <!-- Header -->
                    <tr>
                      <td style="background:#1A9E5C;padding:24px 40px;">
                        <span style="font-size:22px;font-weight:700;color:#ffffff;letter-spacing:-0.5px;">Faaz</span>
                      </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                      <td style="padding:40px 40px 32px;">
                        <h1 style="margin:0 0 8px;font-size:22px;font-weight:700;color:#1F2633;">{heading}</h1>
                        <p style="margin:0 0 24px;font-size:15px;color:#555555;">{greeting}</p>
                        <div style="font-size:15px;color:#555555;line-height:1.6;">
                          {bodyHtml}
                        </div>
                        {ctaBlock}
                        <p style="margin:0;font-size:14px;color:#555555;">
                          Thanks,<br/>
                          <strong style="color:#1F2633;">The Faaz Team</strong>
                        </p>
                      </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                      <td style="background:#F8F6F2;padding:20px 40px;border-top:1px solid #E0DBD0;">
                        <p style="margin:0;font-size:12px;color:#A0A09C;text-align:center;">
                          © Faaz Ltd ·
                          <a href="{_frontendBaseUrl}/privacy" style="color:#A0A09C;">Privacy Policy</a> ·
                          <a href="{_frontendBaseUrl}/terms" style="color:#A0A09C;">Terms of Service</a>
                        </p>
                      </td>
                    </tr>

                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
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

        var username = _config["Smtp:Username"];
        var password = _config["Smtp:Password"];

        using var client = new SmtpClient();
        try
        {
            var socketOptions = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(host, port, socketOptions, ct);
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                await client.AuthenticateAsync(username, password, ct);
            await client.SendAsync(message, ct);
            _logger.LogInformation("Email sent to {To} — {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SMTP delivery failed for {To} — host={Host}:{Port} useSsl={UseSsl}. " +
                "Check Smtp:Username / Smtp:Password in user-secrets and that the Gmail App Password is still active.",
                to, host, port, useSsl);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}
