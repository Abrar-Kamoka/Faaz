namespace Faaz.Services.Notification.Domain;

public static class NotificationEnums
{
    public enum NotificationChannel
    {
        Email = 1,
        InApp = 2
    }

    public enum NotificationStatus
    {
        Pending = 1,
        Sent    = 2,
        Failed  = 3
    }
}
