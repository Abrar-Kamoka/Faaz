using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;

namespace Faaz.Services.Booking.WebHost.Consumers
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    public class PaymentFailedConsumer : IConsumer<PaymentFailedEvent>
    {
        private readonly IBookingServices _bookingServices;

        public PaymentFailedConsumer(IBookingServices b) { _bookingServices = b; }

        public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
        {
            var msg     = context.Message;
            var booking = await _bookingServices.GetByIdAsync(msg.BookingId);
            if (booking is null) return;

            // Don't re-process if already in a terminal or cancelled state
            if ((int)booking.Status >= 10) return;

            var prevStatus = booking.Status;
            booking.Status = BookingStatus.CancelledPaymentFailed;

            await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
            {
                BookingId  = booking.Id,
                FromStatus = prevStatus,
                ToStatus   = BookingStatus.CancelledPaymentFailed,
                ChangedAt  = DateTime.UtcNow,
                Notes      = $"Payment failed: {msg.FailureMessage}"
            });

            await _bookingServices.SaveChangesAsync();
        }
    }
}
