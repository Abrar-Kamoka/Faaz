using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.SharedKernel.Exceptions;
using Faaz.SharedKernel.IntegrationEvents;
using Hangfire;
using MassTransit;
using MediatR;

namespace Faaz.Services.Booking.WebHost.Features.Bookings.Commands
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;
    using static global::Faaz.Services.Booking.Domain.BookingEnums;
    using Faaz.Services.Booking.WebHost.Jobs;

    public class AcceptBookingCommand : IRequest
    {
        public Guid BookingId { get; set; }
        public Guid ConsultantUserId { get; set; }
    }

    public class AcceptBookingCommandHandler : IRequestHandler<AcceptBookingCommand>
    {
        private readonly IBookingServices _bookingServices;
        private readonly ISessionServices _sessionServices;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IBackgroundJobClient _jobs;

        public AcceptBookingCommandHandler(IBookingServices b, ISessionServices s, IPublishEndpoint p, IBackgroundJobClient j)
        { _bookingServices = b; _sessionServices = s; _publishEndpoint = p; _jobs = j; }

        public async Task Handle(AcceptBookingCommand command, CancellationToken ct)
        {
            var booking = await _bookingServices.GetByIdAsync(command.BookingId, ct)
                ?? throw new NotFoundException(nameof(Booking), command.BookingId);

            if (booking.ConsultantUserId != command.ConsultantUserId)
                throw new ForbiddenException("You are not the consultant for this booking.");

            if (booking.Status != BookingStatus.PendingConfirmation)
                throw BusinessRuleException.Error($"Booking cannot be accepted in status {booking.Status}.", "booking.invalid-status");

            booking.Status     = BookingStatus.Confirmed;
            booking.AcceptedAt = DateTime.UtcNow;
            booking.ExpiresAt  = null;

            await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
            {
                BookingId       = booking.Id, FromStatus = BookingStatus.PendingConfirmation,
                ToStatus        = BookingStatus.Confirmed, ChangedByUserId = command.ConsultantUserId,
                Notes           = "Consultant accepted"
            }, ct);

            var sessionSrNo = await _sessionServices.NewSerialNumberAsync(ct);
            var session = new Session
            {
                SrNo            = sessionSrNo,
                BookingId       = booking.Id,
                LiveKitRoomName = $"faaz-session-{booking.Id:N}",
                Status          = SessionStatus.Scheduled
            };
            await _sessionServices.AddAsync(session, ct);
            await _sessionServices.SaveChangesAsync(ct);

            var start    = booking.ScheduledStartUtc;
            var duration = booking.DurationMinutes;

            var createRoomJobId = _jobs.Schedule<ICreateSessionRoomJob>(j => j.ExecuteAsync(booking.Id), start.AddMinutes(-5));
            var noShowJobId     = _jobs.Schedule<INoShowCheckJob>(j => j.ExecuteAsync(booking.Id), start.AddMinutes(15));
            // Hard cutoff exactly at the booked end time — no grace period. A grace window here would
            // ripple into payment/payout timing, no-show/attendance tracking, and the consultant's next
            // booking potentially starting while they're still in this room; deliberately not doing that.
            var forceCloseJobId = _jobs.Schedule<IForceCloseRoomJob>(j => j.ExecuteAsync(booking.Id), start.AddMinutes(duration));

            _jobs.Schedule<ISendSessionReminderJob>(j => j.ExecuteAsync(booking.Id, "T24h"),   start.AddHours(-24));
            _jobs.Schedule<ISendSessionReminderJob>(j => j.ExecuteAsync(booking.Id, "T1h"),    start.AddHours(-1));
            _jobs.Schedule<ISendSessionReminderJob>(j => j.ExecuteAsync(booking.Id, "T15min"), start.AddMinutes(-15));
            _jobs.Schedule<ISendSessionReminderJob>(j => j.ExecuteAsync(booking.Id, "T5min"),  start.AddMinutes(-5));

            session.CreateRoomJobId = createRoomJobId;
            session.NoShowJobId     = noShowJobId;
            session.ForceCloseJobId = forceCloseJobId;

            // Published before either SaveChangesAsync so the EF outbox captures it atomically.
            await _publishEndpoint.Publish(new BookingConfirmedEvent(
                booking.Id, booking.ConsultantUserId, booking.StudentUserId,
                new DateTimeOffset(booking.ScheduledStartUtc, TimeSpan.Zero)), ct);

            await _sessionServices.SaveChangesAsync(ct);
            await _bookingServices.SaveChangesAsync(ct);
        }
    }
}
