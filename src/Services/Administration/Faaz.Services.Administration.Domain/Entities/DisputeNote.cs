using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Administration.Domain.Entities;

public class DisputeNote : BaseSoftDeleteModel
{
    public DisputeNote()
    {
        Id        = RT.Comb.Provider.Sql.Create();
        CreatedAt = DateTime.UtcNow;
    }

    public Guid     BookingId     { get; set; }
    public Guid     AuthorAdminId { get; set; }
    public string   Content       { get; set; } = string.Empty;
    public new DateTime CreatedAt { get; set; }
}
