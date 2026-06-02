namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;

public class UpdateAvailabilityDto
{
    public required List<WeeklySlotDto> WeeklySlots { get; set; }
    public required List<BlockedDateDto> BlockedDates { get; set; }
}
