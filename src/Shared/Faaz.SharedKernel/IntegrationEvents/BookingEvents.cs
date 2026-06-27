namespace Faaz.SharedKernel.IntegrationEvents;

public record BookingRequestReceivedEvent(Guid BookingId, Guid ConsultantId, Guid StudentId, DateTimeOffset SlotStartUtc);
public record BookingConfirmedEvent(Guid BookingId, Guid ConsultantId, Guid StudentId, DateTimeOffset SessionStartUtc);
public record BookingCancelledEvent(Guid BookingId, string CancelledBy, string Reason, bool RefundRequired, decimal RefundAmount);
public record BookingDisputedEvent(Guid BookingId, Guid StudentId, Guid ConsultantId, string Reason);
public record SessionReminderEvent(Guid BookingId, Guid ConsultantId, Guid StudentId, DateTimeOffset SessionStartUtc, string ReminderType);
public record SessionNoShowEvent(Guid BookingId, bool StudentJoined, bool ConsultantJoined);
public record PayoutReleasedEvent(Guid BookingId, Guid ConsultantId, decimal NetAmount);
public record PayoutFailedEvent(Guid BookingId, Guid ConsultantId, decimal Amount, string FailureReason);
public record RefundIssuedEvent(Guid BookingId, Guid StudentId, decimal TotalRefunded, string Reason);
public record RefundAppealApprovedEvent(
    Guid    BookingId,
    Guid    AppealId,
    Guid    StudentUserId,
    decimal RefundAmountGbp,
    Guid    ApprovedByAdminId);
