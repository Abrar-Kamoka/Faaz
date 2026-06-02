namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;

public class UpdateCallPreferencesDto
{
    public required int CallPreference { get; set; }
    public required int MinBookingNoticeHours { get; set; }
    public required int MaxAdvanceBookingDays { get; set; }
}
