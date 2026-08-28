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

    // Weekly slot fields — populated when IsBlockedDate = false. Wall-clock times in the
    // consultant's own timezone (ConsultantProfile.TimeZoneId), NOT UTC — a fixed UTC instant can't
    // represent a recurring weekly slot correctly once DST is involved, since the true UTC offset
    // shifts twice a year in any zone that observes it. Convert with TimeZoneInfo per-date instead
    // of storing a precomputed UTC time.
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeOnly? StartTimeLocal { get; set; }
    public TimeOnly? EndTimeLocal { get; set; }

    // Blocked date fields — populated when IsBlockedDate = true
    public DateOnly? Date { get; set; }
    public string? Reason { get; set; }

    public ConsultantProfile Profile { get; set; } = null!;
}
