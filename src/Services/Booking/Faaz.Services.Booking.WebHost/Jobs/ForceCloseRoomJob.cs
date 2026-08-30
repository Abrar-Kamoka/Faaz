using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.Infrastructure.Services;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Booking.WebHost.Jobs
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    public class ForceCloseRoomJob : IForceCloseRoomJob
    {
        private readonly IBookingServices _bookingServices;
        private readonly ISessionServices _sessionServices;
        private readonly IVideoService _videoService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ForceCloseRoomJob> _logger;

        public ForceCloseRoomJob(IBookingServices b, ISessionServices s, IVideoService v, IPublishEndpoint p, ILogger<ForceCloseRoomJob> l)
        { _bookingServices = b; _sessionServices = s; _videoService = v; _publishEndpoint = p; _logger = l; }

        public async Task ExecuteAsync(Guid bookingId)
        {
            var booking = await _bookingServices.GetByIdWithDetailsAsync(bookingId);
            if (booking is null) return;

            if (booking.Status is not (BookingStatus.InProgress or BookingStatus.Confirmed)) return;

            var session = booking.Session;
            if (session is not null)
            {
                try { await _videoService.DeleteRoomAsync(session.LiveKitRoomName); } catch (Exception ex) { _logger.LogWarning(ex, "Could not delete room {Room}", session.LiveKitRoomName); }

                session.Status        = SessionStatus.Completed;
                session.ActualEndUtc  = DateTime.UtcNow;
            }

            booking.Status      = BookingStatus.Completed;
            booking.CompletedAt = DateTime.UtcNow;

            var actual = session?.ActualStartUtc.HasValue == true
                ? (int)(session.ActualEndUtc!.Value - session.ActualStartUtc!.Value).TotalSeconds
                : booking.DurationMinutes * 60;

            // Published before either SaveChangesAsync so the EF outbox captures it atomically.
            await _publishEndpoint.Publish(new SessionCompletedEvent(bookingId, DateTimeOffset.UtcNow, actual));

            if (session is not null)
                await _sessionServices.SaveChangesAsync();
            await _bookingServices.SaveChangesAsync();

            _logger.LogInformation("Force-closed room and marked booking {Id} as Completed", bookingId);
        }
    }
}
