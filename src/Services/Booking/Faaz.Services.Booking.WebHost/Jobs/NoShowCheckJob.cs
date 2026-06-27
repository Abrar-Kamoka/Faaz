using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Booking.WebHost.Jobs
{
    using static Faaz.Services.Booking.Domain.BookingEnums;

    public class NoShowCheckJob : INoShowCheckJob
    {
        private readonly IBookingServices _bookingServices;
        private readonly ISessionParticipantServices _participantServices;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<NoShowCheckJob> _logger;

        public NoShowCheckJob(IBookingServices b, ISessionParticipantServices p, IPublishEndpoint pub, ILogger<NoShowCheckJob> l)
        { _bookingServices = b; _participantServices = p; _publishEndpoint = pub; _logger = l; }

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

                _logger.LogWarning("NoShow for booking {Id}: {Status}", bookingId, noShowStatus);
                await _publishEndpoint.Publish(new SessionNoShowEvent(bookingId, studentJoined, consultantJoined));

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
            }
        }
    }
}
