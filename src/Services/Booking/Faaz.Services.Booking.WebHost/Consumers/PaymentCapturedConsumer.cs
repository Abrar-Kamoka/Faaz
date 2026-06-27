using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;

namespace Faaz.Services.Booking.WebHost.Consumers
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    public class PaymentCapturedConsumer : IConsumer<PaymentCapturedEvent>
    {
        private readonly IBookingServices _bookingServices;

        public PaymentCapturedConsumer(IBookingServices b) { _bookingServices = b; }

        public async Task Consume(ConsumeContext<PaymentCapturedEvent> context)
        {
            var msg     = context.Message;
            var booking = await _bookingServices.GetByIdAsync(msg.BookingId);
            if (booking is null) return;

            if (booking.Status != BookingStatus.PendingConfirmation) return;

            var prevStatus                = booking.Status;
            booking.Status                = BookingStatus.Confirmed;
            booking.StripePaymentIntentId = msg.StripePaymentIntentId;

            await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
            {
                BookingId  = booking.Id,
                FromStatus = prevStatus,
                ToStatus   = BookingStatus.Confirmed,
                ChangedAt  = DateTime.UtcNow,
                Notes      = $"Payment captured: {msg.StripePaymentIntentId}"
            });

            await _bookingServices.SaveChangesAsync();
        }
    }
}
