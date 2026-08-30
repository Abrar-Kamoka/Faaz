using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.Infrastructure.Services;
using Faaz.Services.Booking.WebHost.Features.Bookings.DTOs;
using Faaz.SharedKernel.Exceptions;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using MediatR;

namespace Faaz.Services.Booking.WebHost.Features.Bookings.Commands
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;
    using static global::Faaz.Services.Booking.Domain.BookingEnums;

    public class RescheduleBookingCommand : IRequest
    {
        public Guid BookingId { get; set; }
        public Guid RequestingStudentId { get; set; }
        public RescheduleBookingDto PostModel { get; set; } = null!;
    }

    public class RescheduleBookingCommandHandler : IRequestHandler<RescheduleBookingCommand>
    {
        private readonly IBookingServices _bookingServices;
        private readonly IBookingConsultantClient _consultantClient;
        private readonly ISlotLockService _slotLock;
        private readonly IPublishEndpoint _publishEndpoint;

        public RescheduleBookingCommandHandler(
            IBookingServices b, IBookingConsultantClient c, ISlotLockService s, IPublishEndpoint p)
        { _bookingServices = b; _consultantClient = c; _slotLock = s; _publishEndpoint = p; }

        public async Task Handle(RescheduleBookingCommand command, CancellationToken ct)
        {
            var booking = await _bookingServices.GetByIdAsync(command.BookingId, ct)
                ?? throw new NotFoundException(nameof(Booking), command.BookingId);

            if (booking.StudentUserId != command.RequestingStudentId)
                throw new ForbiddenException("You are not the student on this booking.");

            var reschedulableStatuses = new[] { BookingStatus.PendingConfirmation, BookingStatus.Confirmed };
            if (!reschedulableStatuses.Contains(booking.Status))
                throw BusinessRuleException.Error($"Cannot reschedule a booking in status {booking.Status}.", "booking.invalid-status");

            // Same notice window as the cancellation policy's lowest tier — prevents a reschedule
            // being used to dodge the "under 24h = no refund" cancellation rule at the last minute.
            var hoursUntilCurrentStart = (booking.ScheduledStartUtc - DateTime.UtcNow).TotalHours;
            if (hoursUntilCurrentStart < 24)
                throw BusinessRuleException.Error("Bookings can only be rescheduled more than 24 hours before the session.", "booking.reschedule-too-late");

            var newStart = command.PostModel.NewScheduledStartUtc;
            if (newStart <= DateTime.UtcNow)
                throw BusinessRuleException.Error("The new time must be in the future.", "booking.invalid-slot");

            var slotCheck = await _consultantClient.CheckSlotAvailabilityAsync(booking.ConsultantProfileId, booking.SessionTypeId, newStart, ct);
            if (slotCheck is null || !slotCheck.IsAvailable)
                throw BusinessRuleException.Error("The selected time slot is not available.", "slot.unavailable");

            var lockKey = $"slot:{booking.ConsultantProfileId}:{newStart:yyyyMMddHHmm}";
            var acquired = await _slotLock.TryAcquireAsync(lockKey, TimeSpan.FromMinutes(10), ct);
            if (!acquired)
                throw BusinessRuleException.Error("The selected time slot was just taken. Please choose another.", "slot.taken");

            if (await _bookingServices.IsSlotTakenAsync(booking.ConsultantProfileId, newStart, ct))
            {
                await _slotLock.ReleaseAsync(lockKey, ct);
                throw BusinessRuleException.Error("The selected time slot is no longer available.", "slot.taken");
            }

            var oldStart = booking.ScheduledStartUtc;
            var fromStatus = booking.Status;

            booking.ScheduledStartUtc = newStart;
            booking.ScheduledEndUtc   = newStart.AddMinutes(booking.DurationMinutes);
            // A rescheduled time is a new commitment for the consultant — require them to
            // re-confirm it, exactly like a brand-new booking request.
            booking.Status    = BookingStatus.PendingConfirmation;
            booking.AcceptedAt = null;
            booking.ExpiresAt  = DateTime.UtcNow.AddHours(12);

            await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
            {
                BookingId       = booking.Id,
                FromStatus      = fromStatus,
                ToStatus        = BookingStatus.PendingConfirmation,
                ChangedByUserId = command.RequestingStudentId,
                Notes           = $"Rescheduled from {oldStart:u} to {newStart:u} — awaiting consultant re-confirmation"
            }, ct);
            // Published before SaveChangesAsync so the EF outbox captures it atomically.
            await _publishEndpoint.Publish(new BookingRescheduledEvent(
                booking.Id, booking.ConsultantUserId, booking.StudentUserId,
                new DateTimeOffset(oldStart, TimeSpan.Zero), new DateTimeOffset(newStart, TimeSpan.Zero)), ct);

            await _bookingServices.SaveChangesAsync(ct);
        }
    }
}
