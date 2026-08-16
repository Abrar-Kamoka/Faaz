using Faaz.SharedKernel.Entities;
using static Faaz.Services.Booking.Domain.BookingEnums;

namespace Faaz.Services.Booking.Domain.Entities;

public class SessionParticipant : BaseSoftDeleteModel
{
    public SessionParticipant()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid     BookingId                { get; set; }
    public Guid     SessionId                { get; set; }
    public Guid     UserId                   { get; set; }
    public ParticipantRole Role              { get; set; }
    public DateTime? FirstJoinedUtc          { get; set; }
    // Set when this participant joins and cleared when they leave — tracks the currently-open
    // connection window so its duration can be added to TotalSecondsInRoom once it closes.
    public DateTime? LastJoinWindowStartUtc  { get; set; }
    public DateTime? LastLeftUtc             { get; set; }
    public int      TotalSecondsInRoom       { get; set; } = 0;
    public int      DisconnectionCount       { get; set; } = 0;
    public bool     CompletedPreSessionCheck { get; set; } = false;
    public string?  PendingReconnectionJobId { get; set; }
    public ParticipantConnectionStatus FinalStatus { get; set; } = ParticipantConnectionStatus.NeverJoined;

    public Session Session { get; set; } = null!;
}

