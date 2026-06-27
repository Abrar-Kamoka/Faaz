using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.SharedKernel.Exceptions;
using Faaz.SharedKernel.IntegrationEvents;
using MassTransit;
using MediatR;

namespace Faaz.Services.Booking.WebHost.Features.Bookings.Commands
{
    using static global::Faaz.Services.Booking.Domain.BookingEnums;

    public class ApproveRefundAppealCommand : IRequest
    {
        public Guid AppealId { get; set; }
        public Guid AdminUserId { get; set; }
        public string? AdminNotes { get; set; }
    }

    public class ApproveRefundAppealCommandHandler : IRequestHandler<ApproveRefundAppealCommand>
    {
        private readonly IRefundAppealServices _appealServices;
        private readonly IPublishEndpoint _publishEndpoint;

        public ApproveRefundAppealCommandHandler(IRefundAppealServices a, IPublishEndpoint p)
        { _appealServices = a; _publishEndpoint = p; }

        public async Task Handle(ApproveRefundAppealCommand command, CancellationToken ct)
        {
            var appeal = await _appealServices.GetByIdAsync(command.AppealId, ct)
                ?? throw new NotFoundException(nameof(RefundAppeal), command.AppealId);

            if (appeal.Status != RefundAppealStatus.Pending)
                throw BusinessRuleException.Error($"Appeal is already {appeal.Status}.", "appeal.already-reviewed");

            appeal.Status            = RefundAppealStatus.Approved;
            appeal.ReviewedByAdminId = command.AdminUserId;
            appeal.ReviewedAt        = DateTime.UtcNow;
            appeal.AdminNotes        = command.AdminNotes;

            await _appealServices.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new RefundAppealApprovedEvent(
                appeal.BookingId, appeal.Id, appeal.StudentUserId,
                appeal.RequestedAmountGbp, command.AdminUserId), ct);
        }
    }
}
