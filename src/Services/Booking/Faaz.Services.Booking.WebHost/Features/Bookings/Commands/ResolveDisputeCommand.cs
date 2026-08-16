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

    public class ResolveDisputeCommand : IRequest
    {
        public Guid BookingId { get; set; }
        public Guid AdminUserId { get; set; }
        public ResolveDisputeDto PostModel { get; set; } = null!;
    }

    public class ResolveDisputeCommandHandler : IRequestHandler<ResolveDisputeCommand>
    {
        private static readonly string[] ValidResolutions = ["favour_student", "favour_consultant", "no_action"];

        private readonly IBookingServices _bookingServices;
        private readonly IPublishEndpoint _publishEndpoint;

        public ResolveDisputeCommandHandler(IBookingServices b, IPublishEndpoint p)
        { _bookingServices = b; _publishEndpoint = p; }

        public async Task Handle(ResolveDisputeCommand command, CancellationToken ct)
        {
            var booking = await _bookingServices.GetByIdAsync(command.BookingId, ct)
                ?? throw new NotFoundException(nameof(Booking), command.BookingId);

            if (booking.Status != BookingStatus.Disputed)
                throw BusinessRuleException.Error("This booking does not have an open dispute.", "dispute.not-open");

            var resolution = command.PostModel.Resolution;
            if (!ValidResolutions.Contains(resolution))
                throw BusinessRuleException.Error("Invalid resolution outcome.", "dispute.invalid-resolution");

            if (string.IsNullOrWhiteSpace(command.PostModel.Note))
                throw BusinessRuleException.Error("A resolution note is required.", "dispute.note-required");

            var fromStatus  = booking.Status;
            var refundAmount = resolution == "favour_student" ? booking.TotalChargedGbp : 0m;

            booking.DisputeResolution     = resolution;
            booking.DisputeResolutionNote = command.PostModel.Note;
            booking.DisputeResolvedAt     = DateTime.UtcNow;

            if (resolution == "favour_student")
            {
                // No payout has been released yet (dispute can only be filed before the settlement sweep) —
                // close the booking out entirely; the refund itself is issued by Payment's DisputeResolvedConsumer.
                booking.Status           = BookingStatus.Settled;
                booking.SettledAt        = DateTime.UtcNow;
                booking.RefundPercentage = 100;
            }
            else
            {
                // favour_consultant / no_action: no fault requiring a refund — return the booking to Completed
                // so the existing payout sweep (ReleasePendingPayoutsJob) picks it up and pays the consultant normally.
                booking.Status = BookingStatus.Completed;
            }

            await _bookingServices.AddStatusHistoryAsync(new BookingStatusHistory
            {
                BookingId       = booking.Id, FromStatus = fromStatus, ToStatus = booking.Status,
                ChangedByUserId = command.AdminUserId,
                Notes           = $"Dispute resolved: {resolution}. {command.PostModel.Note}"
            }, ct);
            await _bookingServices.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new DisputeResolvedEvent(
                booking.Id, booking.StudentUserId, booking.ConsultantUserId,
                resolution, refundAmount, command.PostModel.Note, command.AdminUserId), ct);
        }
    }
}
