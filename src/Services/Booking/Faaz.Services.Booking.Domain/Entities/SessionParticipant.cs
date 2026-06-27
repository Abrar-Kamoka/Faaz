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
    public DateTime? LastLeftUtc             { get; set; }
    public int      TotalSecondsInRoom       { get; set; } = 0;
    public int      DisconnectionCount       { get; set; } = 0;
    public bool     CompletedPreSessionCheck { get; set; } = false;
    public string?  PendingReconnectionJobId { get; set; }
    public ParticipantConnectionStatus FinalStatus { get; set; } = ParticipantConnectionStatus.NeverJoined;
    public string?  Remarks                  { get; set; }
    public string?  ExtraField1              { get; set; }
    public string?  ExtraField2              { get; set; }

    public Session Session { get; set; } = null!;
}

