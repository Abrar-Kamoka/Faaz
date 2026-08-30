using Faaz.Services.Booking.Infrastructure.Interfaces;
using Faaz.Services.Booking.Infrastructure.Services;
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
        private readonly IBookingIdentityClient _identityClient;

        public GetConsultantReviewsQueryHandler(IReviewServices r, IBookingIdentityClient identityClient)
        { _reviewServices = r; _identityClient = identityClient; }

        public async Task<(IReadOnlyList<ReviewDto> Items, int TotalCount)> Handle(GetConsultantReviewsQuery query, CancellationToken ct)
        {
            var (items, total) = await _reviewServices.GetByConsultantProfileIdAsync(query.ConsultantProfileId, query.Page, query.PageSize, ct);
            var publicItems = items.Where(r => r.IsPublic).ToList();

            // Reviews don't own user profile data — best-effort enrichment from Identity, one lookup
            // per distinct student rather than per review; a lookup failure just leaves the name blank.
            var distinctStudentIds = publicItems.Select(r => r.StudentUserId).Distinct().ToList();
            var lookups = await Task.WhenAll(distinctStudentIds.Select(id => _identityClient.GetUserNameAsync(id, ct)));
            var namesByStudentId = new Dictionary<Guid, string>();
            for (var i = 0; i < distinctStudentIds.Count; i++)
                if (lookups[i] is { } name) namesByStudentId[distinctStudentIds[i]] = name.FullName;

            var dtos = publicItems
                .Select(r => new ReviewDto
                {
                    Id                  = r.Id,
                    BookingId           = r.BookingId,
                    StudentUserId       = r.StudentUserId,
                    ConsultantProfileId = r.ConsultantProfileId,
                    Rating              = (int)r.Rating,
                    ReviewText          = r.ReviewText,
                    IsPublic            = r.IsPublic,
                    CreatedAt           = r.CreatedAt,
                    StudentName         = namesByStudentId.GetValueOrDefault(r.StudentUserId)
                }).ToList();

            return (dtos, total);
        }
    }

    public class GetAllReviewsAdminQuery : IRequest<(IReadOnlyList<AdminReviewDto> Items, int TotalCount)>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetAllReviewsAdminQueryHandler : IRequestHandler<GetAllReviewsAdminQuery, (IReadOnlyList<AdminReviewDto> Items, int TotalCount)>
    {
        private readonly IReviewServices _reviewServices;
        private readonly IBookingIdentityClient _identityClient;

        public GetAllReviewsAdminQueryHandler(IReviewServices r, IBookingIdentityClient identityClient)
        { _reviewServices = r; _identityClient = identityClient; }

        public async Task<(IReadOnlyList<AdminReviewDto> Items, int TotalCount)> Handle(GetAllReviewsAdminQuery query, CancellationToken ct)
        {
            var (items, total) = await _reviewServices.GetAllForAdminAsync(query.Page, query.PageSize, ct);

            // Reviews don't own user profile data — best-effort enrichment from Identity, one lookup
            // per distinct user (student or consultant) rather than per review.
            var distinctUserIds = items.Select(r => r.StudentUserId)
                .Concat(items.Select(r => r.Booking.ConsultantUserId))
                .Distinct().ToList();
            var lookups = await Task.WhenAll(distinctUserIds.Select(id => _identityClient.GetUserNameAsync(id, ct)));
            var namesByUserId = new Dictionary<Guid, string>();
            for (var i = 0; i < distinctUserIds.Count; i++)
                if (lookups[i] is { } name) namesByUserId[distinctUserIds[i]] = name.FullName;

            var dtos = items.Select(r => new AdminReviewDto
            {
                Id                  = r.Id,
                BookingId           = r.BookingId,
                StudentUserId       = r.StudentUserId,
                ConsultantProfileId = r.ConsultantProfileId,
                Rating              = (int)r.Rating,
                ReviewText          = r.ReviewText,
                IsPublic            = r.IsPublic,
                IsDeleted           = r.IsDeleted,
                CreatedAt           = r.CreatedAt,
                StudentName         = namesByUserId.GetValueOrDefault(r.StudentUserId),
                ConsultantName      = namesByUserId.GetValueOrDefault(r.Booking.ConsultantUserId)
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
