namespace Faaz.Services.Consultant.Infrastructure.Services;

public interface IBookingReviewClient
{
    Task<ReviewSummaryResult?> GetReviewSummaryAsync(Guid consultantProfileId, CancellationToken ct = default);
}

public record ReviewSummaryResult(decimal AverageRating, int TotalCount);
