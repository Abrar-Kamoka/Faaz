using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Booking.WebHost.Jobs
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    public class ReleasePendingPayoutsJob : IReleasePendingPayoutsJob
    {
        private readonly IBookingServices _bookingServices;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ReleasePendingPayoutsJob> _logger;

        public ReleasePendingPayoutsJob(IBookingServices b, IPublishEndpoint p, ILogger<ReleasePendingPayoutsJob> l)
        { _bookingServices = b; _publishEndpoint = p; _logger = l; }

        public async Task ExecuteAsync()
        {
            var eligible = await _bookingServices.GetPayoutEligibleAsync();
            int count = 0;
            foreach (var booking in eligible)
            {
                var consultantEarnings = booking.TotalChargedGbp - booking.PlatformCommissionGbp;
                await _publishEndpoint.Publish(new PayoutReleasedEvent(
                    booking.Id, booking.ConsultantUserId, consultantEarnings));

                booking.Status    = BookingStatus.Settled;
                booking.SettledAt = DateTime.UtcNow;
                count++;
            }
            if (count > 0)
            {
                await _bookingServices.SaveChangesAsync();
                _logger.LogInformation("Released payouts for {Count} bookings", count);
            }
        }
    }
}
