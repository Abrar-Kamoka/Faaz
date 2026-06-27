using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.WebHost.Features.Reviews.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Booking.WebHost.Features.Reviews.Commands
{
    using Booking = global::Faaz.Services.Booking.Domain.Entities.Booking;
    using static global::Faaz.Services.Booking.Domain.BookingEnums;

    public class SubmitReviewCommand : IRequest<Guid>
    {
        public Guid BookingId { get; set; }
        public Guid RequestingUserId { get; set; }
        public SubmitReviewDto PostModel { get; set; } = null!;
    }

    public class SubmitReviewCommandHandler : IRequestHandler<SubmitReviewCommand, Guid>
    {
        private readonly IBookingServices _bookingServices;
        private readonly IReviewServices _reviewServices;

        public SubmitReviewCommandHandler(IBookingServices b, IReviewServices r) { _bookingServices = b; _reviewServices = r; }

        public async Task<Guid> Handle(SubmitReviewCommand command, CancellationToken ct)
        {
            var booking = await _bookingServices.GetByIdWithDetailsAsync(command.BookingId, ct)
                ?? throw new NotFoundException(nameof(Booking), command.BookingId);

            if (booking.StudentUserId != command.RequestingUserId)
                throw new ForbiddenException("Only the student can submit a review.");

            var reviewableStatuses = new[] { BookingStatus.Completed, BookingStatus.Settled };
            if (!reviewableStatuses.Contains(booking.Status))
                throw BusinessRuleException.Error("A review can only be submitted after a completed session.", "review.not-eligible");

            if (await _reviewServices.ExistsForBookingAsync(command.BookingId, ct))
                throw new ConflictException("A review has already been submitted for this booking.");

            if (booking.Session is null)
                throw BusinessRuleException.Error("No session record found for this booking.", "review.no-session");

            var srNo   = await _reviewServices.NewSerialNumberAsync(ct);
            var review = new Review
            {
                SrNo                = srNo,
                BookingId           = booking.Id,
                SessionId           = booking.Session.Id,
                StudentUserId       = command.RequestingUserId,
                ConsultantProfileId = booking.ConsultantProfileId,
                Rating              = (ReviewRating)command.PostModel.Rating,
                ReviewText          = command.PostModel.ReviewText,
                IsPublic            = true
            };

            await _reviewServices.AddAsync(review, ct);
            await _reviewServices.SaveChangesAsync(ct);
            return review.Id;
        }
    }

    public class SetReviewVisibilityCommand : IRequest
    {
        public Guid ReviewId { get; set; }
        public Guid AdminUserId { get; set; }
        public bool IsPublic { get; set; }
    }

    public class SetReviewVisibilityCommandHandler : IRequestHandler<SetReviewVisibilityCommand>
    {
        private readonly IReviewServices _reviewServices;

        public SetReviewVisibilityCommandHandler(IReviewServices r) { _reviewServices = r; }

        public async Task Handle(SetReviewVisibilityCommand command, CancellationToken ct)
        {
            var review = await _reviewServices.GetByIdAsync(command.ReviewId, ct)
                ?? throw new NotFoundException(nameof(Review), command.ReviewId);
            review.IsPublic = command.IsPublic;
            await _reviewServices.SaveChangesAsync(ct);
        }
    }
}
