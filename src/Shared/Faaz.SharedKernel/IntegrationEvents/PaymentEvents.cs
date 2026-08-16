namespace Faaz.SharedKernel.IntegrationEvents;

// Card authorized/funds held (manual-capture intent reaches requires_capture) — this is what moves
// a booking out of its 10-minute SlotReserved hold, distinct from PaymentCapturedEvent which only
// fires once the consultant accepts and the funds are actually captured.
public record PaymentAuthorizedEvent(Guid BookingId, string StripePaymentIntentId, Guid StudentUserId, decimal Amount);
public record PaymentCapturedEvent(Guid BookingId, string StripePaymentIntentId, decimal Amount);
public record PaymentFailedEvent(Guid BookingId, string StripePaymentIntentId, string FailureMessage);
public record SessionCompletedEvent(Guid BookingId, DateTimeOffset CompletedAt, int ActualDurationSeconds);
