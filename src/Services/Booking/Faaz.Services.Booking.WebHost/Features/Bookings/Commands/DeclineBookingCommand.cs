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

    public class DeclineBookingCommand : IRequest
    {
        public Guid BookingId { get; set; }
        public Guid RequestingConsultantId { get; set; }
        public string? Notes { get; set; }
    }

    public class DeclineBookingCommandHandler : IRequestHandler<DeclineBookingCommand>
    {
        private readonly IBookingServices _bookingServices;
        private readonly IPublishEndpoint _publishEndpoint;

        public DeclineBookingCommandHandler(IBookingServices b, IPublishEndpoint p) { _bookingServices = b; _publishEndpoint = p; }

        public async Task Handle(DeclineBookingCommand command, CancellationToken ct)
        {
            var booking = await _bookingServices.GetByIdAsync(command.BookingId, ct)
                ?? throw new NotFoundException(nameof(Booking), command.BookingId);

            if (booking.ConsultantUserId != command.RequestingConsultantId)
                throw new ForbiddenException("You are not the consultant for this booking.");

            if (booking.Status != BookingStatus.PendingConfirmation)
                throw BusinessRuleException.Error($"Cannot decline in status {booking.Status}.", "booking.invalid-status");

            booking.Status             = BookingStatus.CancelledByConsultant;
            booking.CancellationReason = CancellationReason.ConsultantCancelled;
            booking.CancellationNotes  = command.Notes;
            booking.RefundPercentage   = 100;

            await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
            {
                BookingId       = booking.Id, FromStatus = BookingStatus.PendingConfirmation,
                ToStatus        = BookingStatus.CancelledByConsultant, ChangedByUserId = command.RequestingConsultantId,
                Notes           = command.Notes ?? "Consultant declined"
            }, ct);
            await _bookingServices.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new BookingCancelledEvent(
                booking.Id, $"consultant-{command.RequestingConsultantId}", "Consultant declined",
                RefundRequired: true, RefundAmount: booking.TotalChargedGbp), ct);
        }
    }
}
