using Faaz.Services.Identity.Infrastructure.Interfaces.Token;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Identity.Infrastructure.Services;

internal sealed class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;
    private readonly string _frontendBaseUrl;

    public ConsoleEmailService(IConfiguration config, ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
        _frontendBaseUrl = config["FrontendBaseUrl"] ?? "http://localhost:3000";
    }

    public Task SendEmailVerificationAsync(string toEmail, string firstName, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";
        _logger.LogWarning("[DEV EMAIL] Verification link for {Email}: {Link}", toEmail, link);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string firstName, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        _logger.LogWarning("[DEV EMAIL] Password reset link for {Email}: {Link}", toEmail, link);
        return Task.CompletedTask;
    }

    public Task SendConsultantSetupInviteAsync(string toEmail, string token, CancellationToken ct)
    {
        var link = $"{_frontendBaseUrl}/register/consultant/setup?token={Uri.EscapeDataString(token)}";
        _logger.LogWarning("[DEV EMAIL] Consultant setup invite for {Email}: {Link}", toEmail, link);
        return Task.CompletedTask;
    }

    public Task SendConsultantApprovalAsync(string toEmail, string firstName, CancellationToken ct)
    {
        _logger.LogWarning("[DEV EMAIL] Consultant approved: {Email}", toEmail);
        return Task.CompletedTask;
    }

    public Task SendConsultantRevisionRequestAsync(string toEmail, string firstName, string notes, CancellationToken ct)
    {
        _logger.LogWarning("[DEV EMAIL] Consultant revision requested for {Email}: {Notes}", toEmail, notes);
        return Task.CompletedTask;
    }

    public Task SendConsultantRejectionAsync(string toEmail, string firstName, string reason, CancellationToken ct)
    {
        _logger.LogWarning("[DEV EMAIL] Consultant rejected {Email}: {Reason}", toEmail, reason);
        return Task.CompletedTask;
    }
}
