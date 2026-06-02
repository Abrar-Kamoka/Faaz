using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Consultant.Domain.Entities;

public class ConsultantAvailabilitySlot : BaseSoftDeleteModel
{
    public ConsultantAvailabilitySlot()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid ConsultantProfileId { get; set; }
    public bool IsBlockedDate { get; set; } = false;

    // Weekly slot fields — populated when IsBlockedDate = false
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeOnly? StartTimeUtc { get; set; }
    public TimeOnly? EndTimeUtc { get; set; }

    // Blocked date fields — populated when IsBlockedDate = true
    public DateOnly? Date { get; set; }
    public string? Reason { get; set; }

    public string? Remarks { get; set; }
    public string? ExtraField1 { get; set; }
    public string? ExtraField2 { get; set; }

    public ConsultantProfile Profile { get; set; } = null!;
}
