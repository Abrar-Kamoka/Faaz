using Faaz.SharedKernel.Entities;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.Domain.Entities;

public class NotificationTemplate : BaseEntity
{
    public NotificationTemplate()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    // Matches the integration event's type name (e.g. "BookingConfirmedEvent") — that's what each
    // consumer looks itself up by. Never rename an existing key; add a new template instead.
    public string             Key         { get; set; } = string.Empty;
    public NotificationChannel Channel    { get; set; } = NotificationChannel.InApp;
    public string             Subject     { get; set; } = string.Empty;
    // {{PlaceholderName}} tokens, substituted by NotificationTemplateRenderer — see each consumer's
    // call site for the exact set of placeholders it supplies.
    public string             Body        { get; set; } = string.Empty;
    public string             Description { get; set; } = string.Empty;
}
