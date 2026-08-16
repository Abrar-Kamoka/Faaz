using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Administration.Domain.Entities;

public class PlatformConfig : BaseSoftDeleteModel
{
    public PlatformConfig()
    {
        Id            = RT.Comb.Provider.Sql.Create();
        LastUpdatedAt = DateTime.UtcNow;
    }

    public string   Key                  { get; set; } = string.Empty;
    public string   Value                { get; set; } = string.Empty;
    public string?  Description          { get; set; }
    public DateTime LastUpdatedAt        { get; set; }
    public Guid     LastUpdatedByAdminId { get; set; }
}
