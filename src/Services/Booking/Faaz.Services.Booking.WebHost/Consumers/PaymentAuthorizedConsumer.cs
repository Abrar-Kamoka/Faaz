using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;

namespace Faaz.Services.Booking.WebHost.Consumers
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    // Moves a booking out of its 10-minute SlotReserved hold once the student's card is
    // successfully authorized — without this, a booking never leaves SlotReserved even on a
    // successful payment, and the slot-expiry cleanup job would eventually cancel it regardless.
    public class PaymentAuthorizedConsumer : IConsumer<PaymentAuthorizedEvent>
    {
        private readonly IBookingServices _bookingServices;
        private readonly IPublishEndpoint _publishEndpoint;

        public PaymentAuthorizedConsumer(IBookingServices b, IPublishEndpoint p) { _bookingServices = b; _publishEndpoint = p; }

        public async Task Consume(ConsumeContext<PaymentAuthorizedEvent> context)
        {
            var msg     = context.Message;
            var booking = await _bookingServices.GetByIdAsync(msg.BookingId);
            if (booking is null) return;

            // Idempotent — a redelivered webhook, or one that arrives after the student already
            // cancelled/it already expired, must not resurrect or reprocess the booking.
            if (booking.Status != BookingStatus.SlotReserved) return;

            var prevStatus = booking.Status;
            booking.Status                = BookingStatus.PendingConfirmation;
            booking.StripePaymentIntentId = msg.StripePaymentIntentId;
            booking.ExpiresAt             = DateTime.UtcNow.AddHours(12); // consultant's response window

            await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
            {
                BookingId  = booking.Id,
                FromStatus = prevStatus,
                ToStatus   = BookingStatus.PendingConfirmation,
                ChangedAt  = DateTime.UtcNow,
                Notes      = $"Payment authorized: {msg.StripePaymentIntentId} — awaiting consultant response"
            });

            await _bookingServices.SaveChangesAsync();

            // The booking only becomes a genuine request the consultant should act on now that
            // payment is authorized — notifying any earlier (e.g. at initial SlotReserved creation)
            // would ping the consultant about bookings that were never actually paid for.
            await _publishEndpoint.Publish(new BookingRequestReceivedEvent(
                booking.Id, booking.ConsultantUserId, booking.StudentUserId,
                new DateTimeOffset(booking.ScheduledStartUtc, TimeSpan.Zero)));
        }
    }
}
