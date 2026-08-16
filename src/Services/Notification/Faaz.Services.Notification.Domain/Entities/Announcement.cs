using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Notification.Domain.Entities;

public class Announcement : BaseSoftDeleteModel
{
    public Announcement()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public string  Title           { get; set; } = string.Empty;
    public string  Body            { get; set; } = string.Empty;
    // 0 = All, 1 = Students, 2 = Consultants, 3 = Admins — mirrors the "role" JWT claim's numeric
    // values so filtering "active for me" is a single int comparison, no string mapping.
    public int     Audience        { get; set; }
    public bool    IsActive        { get; set; } = true;
    public DateTime? PublishedAt   { get; set; }
    public DateTime? ExpiresAt     { get; set; }
    public Guid    CreatedByAdminId { get; set; }
}
