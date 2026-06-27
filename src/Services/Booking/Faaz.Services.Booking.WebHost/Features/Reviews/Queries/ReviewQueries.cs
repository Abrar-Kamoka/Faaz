using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.WebHost.Features.Reviews.DTOs;
using MediatR;

namespace Faaz.Services.Booking.WebHost.Features.Reviews.Queries
{
    public class GetConsultantReviewsQuery : IRequest<(IReadOnlyList<ReviewDto> Items, int TotalCount)>
    {
        public Guid ConsultantProfileId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class GetConsultantReviewsQueryHandler : IRequestHandler<GetConsultantReviewsQuery, (IReadOnlyList<ReviewDto> Items, int TotalCount)>
    {
        private readonly IReviewServices _reviewServices;

        public GetConsultantReviewsQueryHandler(IReviewServices r) { _reviewServices = r; }

        public async Task<(IReadOnlyList<ReviewDto> Items, int TotalCount)> Handle(GetConsultantReviewsQuery query, CancellationToken ct)
        {
            var (items, total) = await _reviewServices.GetByConsultantProfileIdAsync(query.ConsultantProfileId, query.Page, query.PageSize, ct);

            var dtos = items
                .Where(r => r.IsPublic)
                .Select(r => new ReviewDto
                {
                    Id                  = r.Id,
                    BookingId           = r.BookingId,
                    StudentUserId       = r.StudentUserId,
                    ConsultantProfileId = r.ConsultantProfileId,
                    Rating              = (int)r.Rating,
                    ReviewText          = r.ReviewText,
                    IsPublic            = r.IsPublic,
                    CreatedAt           = r.CreatedAt
                }).ToList();

            return (dtos, total);
        }
    }

    public class GetReviewSummaryQuery : IRequest<ReviewSummaryDto>
    {
        public Guid ConsultantProfileId { get; set; }
    }

    public class GetReviewSummaryQueryHandler : IRequestHandler<GetReviewSummaryQuery, ReviewSummaryDto>
    {
        private readonly IReviewServices _reviewServices;

        public GetReviewSummaryQueryHandler(IReviewServices r) { _reviewServices = r; }

        public async Task<ReviewSummaryDto> Handle(GetReviewSummaryQuery query, CancellationToken ct)
        {
            var summary = await _reviewServices.GetSummaryAsync(query.ConsultantProfileId, ct);

            return new ReviewSummaryDto
            {
                ConsultantProfileId = query.ConsultantProfileId,
                AverageRating       = summary.AverageRating,
                TotalCount          = summary.TotalCount,
                FiveStarCount       = summary.FiveStarCount,
                FourStarCount       = summary.FourStarCount,
                ThreeStarCount      = summary.ThreeStarCount,
                TwoStarCount        = summary.TwoStarCount,
                OneStarCount        = summary.OneStarCount
            };
        }
    }
}
