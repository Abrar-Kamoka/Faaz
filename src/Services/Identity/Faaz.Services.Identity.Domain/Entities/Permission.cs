using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Identity.Domain.Entities;

public class Permission : BaseEntity
{
    public Permission()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    // Stable machine key checked by [Authorize] policies across services, e.g. "bookings.resolve-dispute".
    // Never rename once shipped — a rename silently strips that permission from every role holding it.
    public string  Key         { get; set; } = string.Empty;
    public string  Category    { get; set; } = string.Empty;
    public string  Description { get; set; } = string.Empty;
}
