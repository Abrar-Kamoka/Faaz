using Faaz.SharedKernel.Entities;
using static Faaz.Services.Booking.Domain.BookingEnums;

namespace Faaz.Services.Booking.Domain.Entities;

public class Session : BaseSoftDeleteModel
{
    public Session()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid          BookingId             { get; set; }
    public string        LiveKitRoomName       { get; set; } = string.Empty;
    public string?       LiveKitRoomSid        { get; set; }
    public SessionStatus Status                { get; set; } = SessionStatus.Scheduled;
    public DateTime?     RoomCreatedAt         { get; set; }
    public DateTime?     ActualStartUtc        { get; set; }
    public DateTime?     ActualEndUtc          { get; set; }
    public int?          ActualDurationSeconds { get; set; }
    public decimal?      CompletionPct         { get; set; }
    public string?       CreateRoomJobId       { get; set; }
    public string?       NoShowJobId           { get; set; }
    public string?       ForceCloseJobId       { get; set; }

    public Booking                        Booking      { get; set; } = null!;
    public ICollection<SessionParticipant> Participants { get; set; } = [];
    public ICollection<SessionEvent>       Events       { get; set; } = [];
}

