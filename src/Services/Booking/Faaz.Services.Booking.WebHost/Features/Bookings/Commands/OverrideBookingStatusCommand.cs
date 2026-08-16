using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.WebHost.Features.Bookings.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Booking.WebHost.Features.Bookings.Commands
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;
    using static global::Faaz.Services.Booking.Domain.BookingEnums;

    public class OverrideBookingStatusCommand : IRequest
    {
        public Guid BookingId { get; set; }
        public Guid AdminUserId { get; set; }
        public OverrideStatusDto PostModel { get; set; } = null!;
    }

    public class OverrideBookingStatusCommandHandler : IRequestHandler<OverrideBookingStatusCommand>
    {
        private readonly IBookingServices _bookingServices;

        public OverrideBookingStatusCommandHandler(IBookingServices b) { _bookingServices = b; }

        public async Task Handle(OverrideBookingStatusCommand command, CancellationToken ct)
        {
            var booking = await _bookingServices.GetByIdAsync(command.BookingId, ct)
                ?? throw new NotFoundException(nameof(Booking), command.BookingId);

            if (!Enum.IsDefined(typeof(BookingStatus), command.PostModel.Status))
                throw BusinessRuleException.Error("Invalid booking status.", "booking.invalid-status");

            var newStatus = (BookingStatus)command.PostModel.Status;
            var fromStatus = booking.Status;

            // A manual support-escalation override — deliberately bypasses the normal lifecycle guards
            // and does NOT publish any integration event, so it never triggers an automatic refund,
            // payout, or notification. Any money movement this implies must be handled separately
            // (e.g. via the Transactions > Refund admin action).
            booking.Status = newStatus;

            await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
            {
                BookingId       = booking.Id, FromStatus = fromStatus, ToStatus = newStatus,
                ChangedByUserId = command.AdminUserId,
                Notes           = $"Admin override: {command.PostModel.Reason ?? "no reason given"}"
            }, ct);
            await _bookingServices.SaveChangesAsync(ct);
        }
    }
}
