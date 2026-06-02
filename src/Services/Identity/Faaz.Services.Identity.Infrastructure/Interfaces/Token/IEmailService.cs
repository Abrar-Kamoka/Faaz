namespace Faaz.Services.Identity.Infrastructure.Interfaces.Token;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string firstName, string token, CancellationToken ct);
    Task SendPasswordResetAsync(string toEmail, string firstName, string token, CancellationToken ct);
    Task SendConsultantSetupInviteAsync(string toEmail, string token, CancellationToken ct);
    Task SendConsultantApprovalAsync(string toEmail, string firstName, CancellationToken ct);
    Task SendConsultantRevisionRequestAsync(string toEmail, string firstName, string notes, CancellationToken ct);
    Task SendConsultantRejectionAsync(string toEmail, string firstName, string reason, CancellationToken ct);
}
