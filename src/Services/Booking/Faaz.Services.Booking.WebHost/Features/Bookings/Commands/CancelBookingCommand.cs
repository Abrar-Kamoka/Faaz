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

    public class CancelBookingCommand : IRequest
    {
        public Guid BookingId { get; set; }
        public Guid RequestingUserId { get; set; }
        public string RequestingRole { get; set; } = "";
        public CancelBookingDto PutModel { get; set; } = null!;
    }

    public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand>
    {
        private readonly IBookingServices _bookingServices;
        private readonly ISlotLockService _slotLock;
        private readonly IPublishEndpoint _publishEndpoint;

        public CancelBookingCommandHandler(IBookingServices b, ISlotLockService s, IPublishEndpoint p) { _bookingServices = b; _slotLock = s; _publishEndpoint = p; }

        public async Task Handle(CancelBookingCommand command, CancellationToken ct)
        {
            var booking = await _bookingServices.GetByIdAsync(command.BookingId, ct)
                ?? throw new NotFoundException(nameof(Booking), command.BookingId);

            var isCancellingStudent    = command.RequestingRole == "1" && booking.StudentUserId == command.RequestingUserId;
            var isCancellingConsultant = command.RequestingRole == "2" && booking.ConsultantUserId == command.RequestingUserId;

            if (!isCancellingStudent && !isCancellingConsultant)
                throw new ForbiddenException("You are not a participant in this booking.");

            var validStatuses = new[] { BookingStatus.SlotReserved, BookingStatus.PendingConfirmation, BookingStatus.Confirmed };
            if (!validStatuses.Contains(booking.Status))
                throw BusinessRuleException.Error($"Cannot cancel in status {booking.Status}.", "booking.invalid-status");

            BookingStatus newStatus;
            int refundPercentage;

            if (booking.Status == BookingStatus.SlotReserved)
            {
                // Never confirmed by the consultant, and never actually captured — this is releasing
                // an in-progress reservation, not cancelling a paid session, so the hours-until-session
                // refund tiers below don't apply. Always a full release, either party.
                newStatus        = isCancellingConsultant ? BookingStatus.CancelledByConsultant : BookingStatus.CancelledByStudent;
                refundPercentage = 100;
            }
            else if (isCancellingConsultant)
            {
                newStatus        = BookingStatus.CancelledByConsultant;
                refundPercentage = 100;
            }
            else
            {
                newStatus = BookingStatus.CancelledByStudent;
                var hoursUntil = (booking.ScheduledStartUtc - DateTime.UtcNow).TotalHours;
                if (hoursUntil > 48)       refundPercentage = 100;
                else if (hoursUntil >= 24) refundPercentage = 50;
                else                        refundPercentage = 0;
            }

            var fromStatus = booking.Status;
            if (fromStatus == BookingStatus.SlotReserved)
            {
                var lockKey = $"slot:{booking.ConsultantProfileId}:{booking.ScheduledStartUtc:yyyyMMddHHmm}";
                await _slotLock.ReleaseAsync(lockKey, ct);
            }

            booking.Status             = newStatus;
            booking.CancellationReason = isCancellingConsultant ? CancellationReason.ConsultantCancelled : CancellationReason.StudentCancelled;
            booking.CancellationNotes  = command.PutModel.Reason;
            booking.RefundPercentage   = refundPercentage;

            await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
            {
                BookingId       = booking.Id, FromStatus = fromStatus, ToStatus = newStatus,
                ChangedByUserId = command.RequestingUserId,
                Notes           = command.PutModel.Reason ?? (isCancellingConsultant ? "Consultant cancelled" : "Student cancelled")
            }, ct);
            await _bookingServices.SaveChangesAsync(ct);

            var refundAmount = Math.Round(booking.TotalChargedGbp * (refundPercentage / 100m), 2);
            await _publishEndpoint.Publish(new BookingCancelledEvent(
                booking.Id,
                isCancellingConsultant ? $"consultant-{command.RequestingUserId}" : $"student-{command.RequestingUserId}",
                command.PutModel.Reason ?? "Cancelled",
                RefundRequired: refundPercentage > 0,
                RefundAmount:   refundAmount), ct);
        }
    }
}
