namespace Faaz.Services.Notification.Infrastructure.Interfaces;

public interface INotificationTemplateRenderer
{
    // Looks up the admin-editable template for "key"; if one exists, substitutes {{Placeholder}}
    // tokens from `placeholders` into its Subject/Body. If none exists (not seeded yet, or deleted),
    // falls back to the caller's own hardcoded text so the notification is never silently dropped.
    Task<(string Subject, string Body)> RenderAsync(
        string key,
        IReadOnlyDictionary<string, string> placeholders,
        string fallbackSubject,
        string fallbackBody,
        CancellationToken ct = default);
}
