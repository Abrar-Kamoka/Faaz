namespace Faaz.SharedKernel.IntegrationEvents;

public record PaymentCapturedEvent(Guid BookingId, string StripePaymentIntentId, decimal Amount);
public record PaymentFailedEvent(Guid BookingId, string StripePaymentIntentId, string FailureMessage);
public record SessionCompletedEvent(Guid BookingId, DateTimeOffset CompletedAt, int ActualDurationSeconds);
