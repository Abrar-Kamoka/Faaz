using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Administration.Domain.Entities;

// Immutable audit trail — extends BaseEntity, NOT BaseSoftDeleteModel.
public class AdminActionLog : BaseEntity
{
    public AdminActionLog()
    {
        Id          = RT.Comb.Provider.Sql.Create();
        PerformedAt = DateTime.UtcNow;
    }

    public Guid                   AdminUserId { get; set; }
    public AdminEnums.AdminAction Action      { get; set; }
    public string                 EntityType  { get; set; } = string.Empty;
    public Guid                   EntityId    { get; set; }
    public string?                Notes       { get; set; }
    public string?                BeforeJson  { get; set; }
    public string?                AfterJson   { get; set; }
    public DateTime               PerformedAt { get; set; }
    public string?                IpAddress   { get; set; }
}
