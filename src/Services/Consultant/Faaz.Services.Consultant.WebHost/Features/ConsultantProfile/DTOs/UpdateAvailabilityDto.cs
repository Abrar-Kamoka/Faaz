namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;

public class UpdateAvailabilityDto
{
    // IANA identifier (e.g. "Europe/London") the WeeklySlots' StartTime/EndTime are relative to.
    public required string TimeZoneId { get; set; }
    public required List<WeeklySlotDto> WeeklySlots { get; set; }
    public required List<BlockedDateDto> BlockedDates { get; set; }
    // Optional — left null when this call is made from a screen that only edits the weekly schedule
    // (the value is then left untouched). Also settable here, not just via /call-preferences, since
    // the availability screen is where consultants actually look for it.
    public int? MinBookingNoticeHours { get; set; }
    public int? MaxAdvanceBookingDays { get; set; }
}
