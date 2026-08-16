using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Consultant.Domain.Entities;

public class ConsultantSessionType : BaseSoftDeleteModel
{
    public ConsultantSessionType()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid ConsultantProfileId { get; set; }
    public string Name { get; set; } = null!;
    public int DurationMinutes { get; set; }
    public decimal PriceGbp { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public ConsultantProfile Profile { get; set; } = null!;
}
