using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.WebHost.Features.Bookings.DTOs;
using Faaz.SharedKernel.Exceptions;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using MediatR;

namespace Faaz.Services.Booking.WebHost.Features.Bookings.Commands
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;
    using static global::Faaz.Services.Booking.Domain.BookingEnums;

    public class FileDisputeCommand : IRequest
    {
        public Guid BookingId { get; set; }
        public Guid RequestingStudentId { get; set; }
        public FileDisputeDto PostModel { get; set; } = null!;
    }

    public class FileDisputeCommandHandler : IRequestHandler<FileDisputeCommand>
    {
        private readonly IBookingServices _bookingServices;
        private readonly IPublishEndpoint _publishEndpoint;

        public FileDisputeCommandHandler(IBookingServices b, IPublishEndpoint p)
        { _bookingServices = b; _publishEndpoint = p; }

        public async Task Handle(FileDisputeCommand command, CancellationToken ct)
        {
            var booking = await _bookingServices.GetByIdAsync(command.BookingId, ct)
                ?? throw new NotFoundException(nameof(Booking), command.BookingId);

            if (booking.StudentUserId != command.RequestingStudentId)
                throw new ForbiddenException("You are not the student for this booking.");

            if (string.IsNullOrWhiteSpace(command.PostModel.Reason))
                throw BusinessRuleException.Error("A reason is required to file a dispute.", "dispute.reason-required");

            // Only a session that finished and hasn't yet been settled (payout not yet released) can be disputed —
            // this mirrors the payout buffer window (ReleasePendingPayoutsJob sweeps Completed bookings after 48h).
            if (booking.Status != BookingStatus.Completed)
                throw BusinessRuleException.Error(
                    "A dispute can only be filed for a completed session that hasn't yet been settled.", "dispute.invalid-status");

            if (booking.SettledAt != null)
                throw BusinessRuleException.Error(
                    "Cannot dispute a session after it has already been settled and the consultant paid.", "dispute.already-settled");

            var fromStatus = booking.Status;
            booking.Status        = BookingStatus.Disputed;
            booking.DisputeReason = command.PostModel.Reason;

            await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
            {
                BookingId       = booking.Id, FromStatus = fromStatus, ToStatus = BookingStatus.Disputed,
                ChangedByUserId = command.RequestingStudentId,
                Notes           = command.PostModel.Reason
            }, ct);
            await _bookingServices.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new BookingDisputedEvent(
                booking.Id, booking.StudentUserId, booking.ConsultantUserId, command.PostModel.Reason), ct);
        }
    }
}
