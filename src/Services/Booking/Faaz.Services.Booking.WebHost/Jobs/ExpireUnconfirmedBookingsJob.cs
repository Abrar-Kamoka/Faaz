using Faaz.Services.Booking.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Booking.WebHost.Jobs
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    public class ExpireUnconfirmedBookingsJob : IExpireUnconfirmedBookingsJob
    {
        private readonly IBookingServices _bookingServices;
        private readonly ILogger<ExpireUnconfirmedBookingsJob> _logger;

        public ExpireUnconfirmedBookingsJob(IBookingServices b, ILogger<ExpireUnconfirmedBookingsJob> l)
        { _bookingServices = b; _logger = l; }

        public async Task ExecuteAsync()
        {
            var expired = await _bookingServices.GetExpiredUnconfirmedAsync();
            foreach (var booking in expired)
            {
                var prev = booking.Status;
                booking.Status             = BookingStatus.CancelledTimeout;
                booking.CancellationReason = Domain.BookingEnums.CancellationReason.Timeout;
                booking.CancellationNotes  = "Auto-expired: consultant did not confirm within the allowed window";
                await _bookingServices.AddStatusHistoryAsync(new Domain.Entities.BookingStatusHistory
                {
                    BookingId  = booking.Id,
                    FromStatus = prev,
                    ToStatus   = BookingStatus.CancelledTimeout,
                    ChangedAt  = DateTime.UtcNow,
                    Notes      = "Auto-expired: consultant did not confirm within the allowed window"
                });
            }
            if (expired.Count > 0)
            {
                await _bookingServices.SaveChangesAsync();
                _logger.LogInformation("Expired {Count} unconfirmed bookings", expired.Count);
            }
        }
    }
}
