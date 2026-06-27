namespace Faaz.Services.Booking.Domain;

public static class BookingEnums
{
    public enum BookingStatus
    {
        SlotReserved               = 0,
        PendingConfirmation        = 1,
        Confirmed                  = 2,
        InProgress                 = 3,
        Completed                  = 4,
        Settled                    = 5,
        CancelledByStudent         = 10,
        CancelledByConsultant      = 11,
        CancelledTimeout           = 12,
        CancelledPaymentFailed     = 13,
        Disputed                   = 20,
        StudentNoShow              = 30,
        ConsultantNoShow           = 31,
        BothNoShow                 = 32,
        StudentConnectionFailed    = 33,
        ConsultantConnectionFailed = 34,
        CompletedEarly             = 41,
        PlatformTechnicalFailure   = 50
    }

    public enum CallType
    {
        VideoAndAudio = 1,
        AudioOnly     = 2
    }

    public enum CancellationReason
    {
        StudentCancelled    = 1,
        ConsultantCancelled = 2,
        Timeout             = 3,
        PaymentFailed       = 4,
        AdminOverride       = 5
    }

    public enum RefundAppealStatus
    {
        Pending  = 0,
        Approved = 1,
        Rejected = 2
    }

    public enum SessionStatus
    {
        Scheduled                  = 1,
        RoomCreating               = 2,
        RoomReady                  = 3,
        WaitingForParticipants     = 4,
        StudentWaiting             = 5,
        ConsultantWaiting          = 6,
        InProgress                 = 7,
        Interrupted                = 8,
        Completed                  = 9,
        CompletedEarly             = 10,
        StudentNoShow              = 11,
        ConsultantNoShow           = 12,
        BothNoShow                 = 13,
        StudentConnectionFailed    = 14,
        ConsultantConnectionFailed = 15,
        PlatformTechnicalFailure   = 16,
        DisputeRaised              = 17,
        Cancelled                  = 18
    }

    public enum SessionEventType
    {
        RoomCreated               = 1,
        RoomDestroyed             = 2,
        ParticipantJoined         = 3,
        ParticipantLeft           = 4,
        ParticipantRejoined       = 5,
        TrackPublished            = 6,
        TrackUnpublished          = 7,
        SessionInterrupted        = 8,
        SessionResumed            = 9,
        ReconnectionWindowExpired = 10,
        NoShowCheckTriggered      = 11,
        SessionCompleted          = 12,
        SessionCompletedEarly     = 13,
        PlatformFailureDetected   = 14
    }

    public enum ParticipantRole
    {
        Student    = 1,
        Consultant = 2
    }

    public enum ParticipantConnectionStatus
    {
        NeverJoined             = 0,
        JoinedAndLeft           = 1,
        JoinedCompleted         = 2,
        DisconnectedBriefly     = 3,
        DisconnectedAndRejoined = 4,
        DisconnectedPermanently = 5,
        ConnectionFailed        = 6
    }

    public enum ReviewRating
    {
        One   = 1,
        Two   = 2,
        Three = 3,
        Four  = 4,
        Five  = 5
    }
}
