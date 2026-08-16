using Faaz.Services.Notification.Infrastructure.DatabaseContext;
using Faaz.Services.Notification.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Faaz.Services.Notification.Infrastructure.Services;

internal sealed class NotificationTemplateRenderer : INotificationTemplateRenderer
{
    private readonly NotificationDbContext _db;

    public NotificationTemplateRenderer(NotificationDbContext db) { _db = db; }

    public async Task<(string Subject, string Body)> RenderAsync(
        string key,
        IReadOnlyDictionary<string, string> placeholders,
        string fallbackSubject,
        string fallbackBody,
        CancellationToken ct = default)
    {
        var template = await _db.NotificationTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Key == key, ct);
        if (template is null) return (fallbackSubject, fallbackBody);

        return (Substitute(template.Subject, placeholders), Substitute(template.Body, placeholders));
    }

    private static string Substitute(string text, IReadOnlyDictionary<string, string> placeholders)
    {
        if (placeholders.Count == 0) return text;

        var sb = new StringBuilder(text);
        foreach (var (name, value) in placeholders)
            sb.Replace("{{" + name + "}}", value);
        return sb.ToString();
    }
}
