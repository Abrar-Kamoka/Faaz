using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.WebHost.Features.Bookings.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Booking.WebHost.Features.Bookings.Commands
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;
    using static global::Faaz.Services.Booking.Domain.BookingEnums;

    public class SubmitRefundAppealCommand : IRequest<Guid>
    {
        public Guid BookingId { get; set; }
        public Guid RequestingStudentId { get; set; }
        public SubmitRefundAppealDto PostModel { get; set; } = null!;
    }

    public class SubmitRefundAppealCommandHandler : IRequestHandler<SubmitRefundAppealCommand, Guid>
    {
        private readonly IBookingServices _bookingServices;
        private readonly IRefundAppealServices _appealServices;

        public SubmitRefundAppealCommandHandler(IBookingServices b, IRefundAppealServices a)
        { _bookingServices = b; _appealServices = a; }

        public async Task<Guid> Handle(SubmitRefundAppealCommand command, CancellationToken ct)
        {
            var booking = await _bookingServices.GetByIdAsync(command.BookingId, ct)
                ?? throw new NotFoundException(nameof(Booking), command.BookingId);

            if (booking.StudentUserId != command.RequestingStudentId)
                throw new ForbiddenException("You are not the student for this booking.");

            if (booking.Status != BookingStatus.CancelledByStudent)
                throw BusinessRuleException.Error(
                    "A refund appeal can only be submitted for a student-cancelled booking.", "appeal.invalid-status");

            if (booking.RefundPercentage == 100)
                throw BusinessRuleException.Error(
                    "You already received a full refund for this booking.", "appeal.already-full-refund");

            if (booking.SettledAt != null)
                throw BusinessRuleException.Error(
                    "Cannot appeal after the booking has been settled and consultant paid.", "appeal.payout-already-released");

            if (await _appealServices.ExistsForBookingAsync(command.BookingId, ct))
                throw new ConflictException("A refund appeal has already been submitted for this booking.");

            var alreadyRefunded = Math.Round(booking.TotalChargedGbp * ((booking.RefundPercentage ?? 0) / 100m), 2);
            var requestedAmount = booking.TotalChargedGbp - alreadyRefunded;

            var appeal = new RefundAppeal
            {
                BookingId          = booking.Id,
                StudentUserId      = command.RequestingStudentId,
                Reason             = command.PostModel.Reason,
                RequestedAmountGbp = requestedAmount,
                Status             = RefundAppealStatus.Pending
            };
            appeal.SrNo = await _appealServices.NewSerialNumberAsync(ct);
            await _appealServices.AddAsync(appeal, ct);
            await _appealServices.SaveChangesAsync(ct);

            return appeal.Id;
        }
    }
}
