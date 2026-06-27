using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Booking.WebHost.Features.Bookings.Commands
{
    using static global::Faaz.Services.Booking.Domain.BookingEnums;

    public class RejectRefundAppealCommand : IRequest
    {
        public Guid AppealId { get; set; }
        public Guid AdminUserId { get; set; }
        public string AdminNotes { get; set; } = string.Empty;
    }

    public class RejectRefundAppealCommandHandler : IRequestHandler<RejectRefundAppealCommand>
    {
        private readonly IRefundAppealServices _appealServices;

        public RejectRefundAppealCommandHandler(IRefundAppealServices a) { _appealServices = a; }

        public async Task Handle(RejectRefundAppealCommand command, CancellationToken ct)
        {
            var appeal = await _appealServices.GetByIdAsync(command.AppealId, ct)
                ?? throw new NotFoundException(nameof(RefundAppeal), command.AppealId);

            if (appeal.Status != RefundAppealStatus.Pending)
                throw BusinessRuleException.Error($"Appeal is already {appeal.Status}.", "appeal.already-reviewed");

            appeal.Status            = RefundAppealStatus.Rejected;
            appeal.ReviewedByAdminId = command.AdminUserId;
            appeal.ReviewedAt        = DateTime.UtcNow;
            appeal.AdminNotes        = command.AdminNotes;

            await _appealServices.SaveChangesAsync(ct);
        }
    }
}
