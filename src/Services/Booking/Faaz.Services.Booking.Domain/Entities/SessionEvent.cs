using Faaz.SharedKernel.Entities;
using static Faaz.Services.Booking.Domain.BookingEnums;

namespace Faaz.Services.Booking.Domain.Entities;

public class SessionEvent : BaseEntity
{
    public SessionEvent()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid             BookingId           { get; set; }
    public Guid             SessionId           { get; set; }
    public string           LiveKitRoomSid      { get; set; } = string.Empty;
    public string           LiveKitEventId      { get; set; } = string.Empty;
    public SessionEventType EventType           { get; set; }
    public string?          ParticipantIdentity { get; set; }
    public ParticipantRole? Role                { get; set; }
    public DateTime         OccurredAtUtc       { get; set; }
    public string?          RawWebhookPayload   { get; set; }
    public string?          Remarks             { get; set; }
    public string?          ExtraField1         { get; set; }
    public string?          ExtraField2         { get; set; }

    public Session Session { get; set; } = null!;
}

