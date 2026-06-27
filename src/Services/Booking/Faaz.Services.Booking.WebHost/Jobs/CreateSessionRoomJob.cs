using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.Infrastructure.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Booking.WebHost.Jobs
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    public class CreateSessionRoomJob : ICreateSessionRoomJob
    {
        private readonly IBookingServices _bookingServices;
        private readonly ISessionServices _sessionServices;
        private readonly IVideoService _videoService;
        private readonly ILogger<CreateSessionRoomJob> _logger;

        public CreateSessionRoomJob(IBookingServices b, ISessionServices s, IVideoService v, ILogger<CreateSessionRoomJob> l)
        { _bookingServices = b; _sessionServices = s; _videoService = v; _logger = l; }

        public async Task ExecuteAsync(Guid bookingId)
        {
            var booking = await _bookingServices.GetByIdWithDetailsAsync(bookingId);
            if (booking is null) { _logger.LogWarning("CreateSessionRoomJob: booking {Id} not found", bookingId); return; }
            if (booking.Status is not (BookingStatus.Confirmed or BookingStatus.PendingConfirmation)) return;

            var session = booking.Session;
            if (session is null) { _logger.LogWarning("CreateSessionRoomJob: no session for booking {Id}", bookingId); return; }

            try
            {
                await _videoService.CreateRoomAsync(session.LiveKitRoomName, booking.DurationMinutes + 30);
                session.Status = SessionStatus.RoomReady;
                await _sessionServices.SaveChangesAsync();
                _logger.LogInformation("Session room created for booking {Id}", bookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create session room for booking {Id}", bookingId);
                throw;
            }
        }
    }
}
