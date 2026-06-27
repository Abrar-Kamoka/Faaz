using Faaz.Services.Booking.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Booking.WebHost.Jobs
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    public class ReconnectionWindowExpiredJob : IReconnectionWindowExpiredJob
    {
        private readonly IBookingServices _bookingServices;
        private readonly ISessionServices _sessionServices;
        private readonly ILogger<ReconnectionWindowExpiredJob> _logger;

        public ReconnectionWindowExpiredJob(IBookingServices b, ISessionServices s, ILogger<ReconnectionWindowExpiredJob> l)
        { _bookingServices = b; _sessionServices = s; _logger = l; }

        public async Task ExecuteAsync(Guid bookingId)
        {
            var booking = await _bookingServices.GetByIdWithDetailsAsync(bookingId);
            if (booking is null) return;
            if (booking.Status != BookingStatus.InProgress) return;

            var session = booking.Session;
            if (session is not null && session.Status == SessionStatus.Interrupted)
            {
                session.Status       = SessionStatus.Completed;
                session.ActualEndUtc = DateTime.UtcNow;
                await _sessionServices.SaveChangesAsync();
            }

            booking.Status      = BookingStatus.Completed;
            booking.CompletedAt = DateTime.UtcNow;
            await _bookingServices.SaveChangesAsync();

            _logger.LogInformation("Reconnection window expired for booking {Id}; marked Completed", bookingId);
        }
    }
}
