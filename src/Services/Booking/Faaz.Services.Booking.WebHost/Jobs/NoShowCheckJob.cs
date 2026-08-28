using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.Infrastructure.Services;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Booking.WebHost.Jobs
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    public class NoShowCheckJob : INoShowCheckJob
    {
        private readonly IBookingServices _bookingServices;
        private readonly ISessionServices _sessionServices;
        private readonly ISessionParticipantServices _participantServices;
        private readonly IVideoService _videoService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<NoShowCheckJob> _logger;

        public NoShowCheckJob(
            IBookingServices b, ISessionServices s, ISessionParticipantServices p, IVideoService v,
            IPublishEndpoint pub, ILogger<NoShowCheckJob> l)
        { _bookingServices = b; _sessionServices = s; _participantServices = p; _videoService = v; _publishEndpoint = pub; _logger = l; }

        public async Task ExecuteAsync(Guid bookingId)
        {
            var booking = await _bookingServices.GetByIdWithDetailsAsync(bookingId);
            if (booking is null) return;
            if (booking.Status is not (BookingStatus.Confirmed or BookingStatus.InProgress)) return;

            var session = booking.Session;
            if (session is null) return;

            var participants     = await _participantServices.GetBySessionIdAsync(session.Id);
            var studentJoined    = participants.Any(p => p.Role == ParticipantRole.Student);
            var consultantJoined = participants.Any(p => p.Role == ParticipantRole.Consultant);

            if (!studentJoined || !consultantJoined)
            {
                var noShowStatus = (!studentJoined && !consultantJoined) ? BookingStatus.BothNoShow
                                 : !studentJoined ? BookingStatus.StudentNoShow
                                 : BookingStatus.ConsultantNoShow;
                var sessionNoShowStatus = (!studentJoined && !consultantJoined) ? SessionStatus.BothNoShow
                                 : !studentJoined ? SessionStatus.StudentNoShow
                                 : SessionStatus.ConsultantNoShow;

                _logger.LogWarning("NoShow for booking {Id}: {Status}", bookingId, noShowStatus);

                // Own the room cleanup here rather than leaving it to ForceCloseRoomJob — that job only
                // acts on bookings still Confirmed/InProgress, so once this method moves the booking to
                // a no-show status, ForceCloseRoomJob would otherwise no-op and leak the LiveKit room.
                try { await _videoService.DeleteRoomAsync(session.LiveKitRoomName); }
                catch (Exception ex) { _logger.LogWarning(ex, "Could not delete room {Room}", session.LiveKitRoomName); }

                session.Status       = sessionNoShowStatus;
                session.ActualEndUtc = DateTime.UtcNow;
                await _sessionServices.SaveChangesAsync();

                var prevStatus = booking.Status;
                booking.Status = noShowStatus;
                await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
                {
                    BookingId  = bookingId,
                    FromStatus = prevStatus,
                    ToStatus   = noShowStatus,
                    ChangedAt  = DateTime.UtcNow,
                    Notes      = $"No-show: studentJoined={studentJoined}, consultantJoined={consultantJoined}"
                });
                await _bookingServices.SaveChangesAsync();

                await _publishEndpoint.Publish(new SessionNoShowEvent(
                    bookingId, booking.ConsultantUserId, booking.StudentUserId, studentJoined, consultantJoined));
            }
        }
    }
}
