using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Booking.WebHost.Jobs
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    public class SendSessionReminderJob : ISendSessionReminderJob
    {
        private readonly IBookingServices _bookingServices;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<SendSessionReminderJob> _logger;

        public SendSessionReminderJob(IBookingServices b, IPublishEndpoint p, ILogger<SendSessionReminderJob> l)
        { _bookingServices = b; _publishEndpoint = p; _logger = l; }

        public async Task ExecuteAsync(Guid bookingId, string reminderType)
        {
            var booking = await _bookingServices.GetByIdAsync(bookingId);
            if (booking is null) return;
            if (booking.Status is not (BookingStatus.Confirmed or BookingStatus.InProgress)) return;

            await _publishEndpoint.Publish(new SessionReminderEvent(
                bookingId, booking.ConsultantUserId, booking.StudentUserId,
                new DateTimeOffset(booking.ScheduledStartUtc, TimeSpan.Zero),
                reminderType));

            // No booking mutation here, but the EF outbox only flushes a published message on a
            // SaveChangesAsync call against the same DbContext — without this, the message above
            // would stay buffered in memory and never reach the broker.
            await _bookingServices.SaveChangesAsync();

            _logger.LogInformation("Session reminder {Type} published for booking {Id}", reminderType, bookingId);
        }
    }
}
