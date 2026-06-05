namespace Faaz.Services.Notification.Infrastructure.Interfaces;

public interface IEmailSenderService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
