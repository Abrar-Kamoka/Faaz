namespace Faaz.Services.Booking.WebHost.Features.Sessions.DTOs;

public class JoinSessionResultDto
{
    public string RoomName { get; set; } = "";
    public string Token { get; set; } = "";
    public string ServerUrl { get; set; } = "";
    public bool PreSessionCheckRequired { get; set; }
}

public class JoinSessionDto { public bool PreSessionCheckCompleted { get; set; } }
