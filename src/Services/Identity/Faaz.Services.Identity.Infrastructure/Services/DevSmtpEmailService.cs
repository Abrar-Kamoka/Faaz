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
            BuildHtml(firstName, "Verify your email address",
                "<p style=\"margin:0 0 16px;\">Thanks for joining Faaz! Please verify your email address to activate your account.</p><p style=\"margin:0 0 24px;\">This link expires in <strong>24 hours</strong>.</p>",
                link, "Verify Email Address"), ct);
    }

    public Task SendPasswordResetAsync(string toEmail, string firstName, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        _logger.LogWarning("[DEV EMAIL] Password reset link for {Email}: {Link}", toEmail, link);
        return TrySendAsync(toEmail, "Reset your Faaz password",
            BuildHtml(firstName, "Reset your password",
                "<p style=\"margin:0 0 16px;\">We received a request to reset the password for your Faaz account.</p><p style=\"margin:0 0 24px;\">This link expires in <strong>15 minutes</strong>.</p>",
                link, "Reset Password"), ct);
    }

    public Task SendConsultantSetupInviteAsync(string toEmail, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/register/consultant/setup?token={Uri.EscapeDataString(token)}";
        _logger.LogWarning("[DEV EMAIL] Consultant setup invite for {Email}: {Link}", toEmail, link);
        return TrySendAsync(toEmail, "You're invited to join Faaz as a consultant",
            BuildHtml(null, "You've been invited to join Faaz",
                "<p style=\"margin:0 0 16px;\">Your application has been reviewed and we'd love to have you on board.</p><p style=\"margin:0 0 24px;\">This invitation expires in <strong>72 hours</strong>.</p>",
                link, "Set Up My Account"), ct);
    }

    public Task SendConsultantApprovalAsync(string toEmail, string firstName, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/app/consultant/dashboard";
        _logger.LogWarning("[DEV EMAIL] Consultant approved: {Email}", toEmail);
        return TrySendAsync(toEmail, "Your Faaz profile has been approved",
            BuildHtml(firstName, "Congratulations — you're approved!",
                "<p style=\"margin:0 0 24px;\">Your Faaz consultant profile has been reviewed and approved. You can now log in and start accepting bookings.</p>",
                link, "Go to My Dashboard"), ct);
    }

    public Task SendConsultantRevisionRequestAsync(string toEmail, string firstName, string notes, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/app/consultant/setup/personal";
        _logger.LogWarning("[DEV EMAIL] Consultant revision requested for {Email}: {Notes}", toEmail, notes);
        return TrySendAsync(toEmail, "Action required: your Faaz application needs updates",
            BuildHtml(firstName, "Your application needs a few changes",
                $"<p style=\"margin:0 0 16px;\">Our team has reviewed your application and would like you to make the following changes:</p><div style=\"margin:0 0 24px;padding:16px;background:#f8f6f2;border-left:4px solid #1A9E5C;border-radius:4px;\"><p style=\"margin:0;font-size:14px;color:#555555;\">{System.Net.WebUtility.HtmlEncode(notes)}</p></div>",
                link, "Update My Profile"), ct);
    }

    public Task SendConsultantRejectionAsync(string toEmail, string firstName, string reason, CancellationToken ct)
    {
        _logger.LogWarning("[DEV EMAIL] Consultant rejected {Email}: {Reason}", toEmail, reason);
        return TrySendAsync(toEmail, "An update on your Faaz application",
            BuildHtml(firstName, "Thank you for applying to Faaz",
                $"<p style=\"margin:0 0 16px;\">Thank you for your interest in joining Faaz. After careful review, we are unable to approve your application at this time.</p><div style=\"margin:0 0 24px;padding:16px;background:#f8f6f2;border-left:4px solid #EF4444;border-radius:4px;\"><p style=\"margin:0;font-size:14px;color:#555555;\">{System.Net.WebUtility.HtmlEncode(reason)}</p></div>",
                null, null), ct);
    }

    private static string BuildHtml(string? firstName, string heading, string bodyHtml, string? ctaUrl, string? ctaLabel)
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
                    <tr>
                      <td style="background:#1A9E5C;padding:24px 40px;">
                        <span style="font-size:22px;font-weight:700;color:#ffffff;letter-spacing:-0.5px;">Faaz</span>
                      </td>
                    </tr>
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
                    <tr>
                      <td style="background:#F8F6F2;padding:20px 40px;border-top:1px solid #E0DBD0;">
                        <p style="margin:0;font-size:12px;color:#A0A09C;text-align:center;">
                          © Faaz Ltd [DEV] · This email was sent by a development environment.
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
