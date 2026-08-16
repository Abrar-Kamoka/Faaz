using Faaz.SharedKernel.Entities;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.Domain.Entities;

public class NotificationLog : BaseSoftDeleteModel
{
    public NotificationLog()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid               UserId    { get; set; }
    public NotificationChannel Channel  { get; set; }
    public string             Type      { get; set; } = string.Empty;
    public string             Subject   { get; set; } = string.Empty;
    public string             Body      { get; set; } = string.Empty;
    public bool               IsRead    { get; set; } = false;
    public NotificationStatus Status    { get; set; } = NotificationStatus.Pending;
    public DateTime?          SentAt    { get; set; }
    public DateTime?          ReadAt    { get; set; }
    public string?            Payload   { get; set; }
}

