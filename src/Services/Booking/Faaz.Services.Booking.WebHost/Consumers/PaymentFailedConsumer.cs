using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;

namespace Faaz.Services.Booking.WebHost.Consumers
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    // Also reached when a capture fails AFTER the consultant already accepted (see
    // BookingConfirmedConsumer) — not just the original pre-authorization failure path. In that case
    // AcceptBookingCommand has already scheduled the session/reminder jobs, but every one of those jobs
    // (CreateSessionRoomJob, NoShowCheckJob, ForceCloseRoomJob, SendSessionReminderJob) already guards on
    // booking.Status being Confirmed/PendingConfirmation/InProgress before doing anything, so flipping the
    // status here to CancelledPaymentFailed is enough to make them all no-op harmlessly — no separate
    // Hangfire job cancellation needed.
    public class PaymentFailedConsumer : IConsumer<PaymentFailedEvent>
    {
        private readonly IBookingServices _bookingServices;
        private readonly IPublishEndpoint _publishEndpoint;

        public PaymentFailedConsumer(IBookingServices b, IPublishEndpoint p) { _bookingServices = b; _publishEndpoint = p; }

        public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
        {
            var msg     = context.Message;
            var booking = await _bookingServices.GetByIdAsync(msg.BookingId);
            if (booking is null) return;

            // Don't re-process if already in a terminal or cancelled state
            if ((int)booking.Status >= 10) return;

            var prevStatus = booking.Status;
            booking.Status             = BookingStatus.CancelledPaymentFailed;
            booking.CancellationReason = CancellationReason.PaymentFailed;
            booking.CancellationNotes  = $"Payment failed: {msg.FailureMessage}";

            await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
            {
                BookingId  = booking.Id,
                FromStatus = prevStatus,
                ToStatus   = BookingStatus.CancelledPaymentFailed,
                ChangedAt  = DateTime.UtcNow,
                Notes      = $"Payment failed: {msg.FailureMessage}"
            });

            await _bookingServices.SaveChangesAsync();

            // Reuse the same notification path every other cancellation uses so both sides actually find
            // out — this matters most for the post-accept capture-failure case, where the consultant
            // otherwise has no way to learn a session they accepted just silently stopped existing.
            await _publishEndpoint.Publish(new BookingCancelledEvent(
                booking.Id, booking.ConsultantUserId, booking.StudentUserId, "system-payment-failed",
                msg.FailureMessage, RefundRequired: false, RefundAmount: 0m));
        }
    }
}
