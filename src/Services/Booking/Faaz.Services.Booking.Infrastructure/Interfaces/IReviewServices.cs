using Faaz.Services.Booking.Domain.Entities;

namespace Faaz.Services.Booking.Infrastructure.Interfaces;

public interface IReviewServices
{
    Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken ct = default);
    Task<Review?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByConsultantProfileIdAsync(Guid profileId, int page, int pageSize, CancellationToken ct = default);
    Task<(decimal AverageRating, int TotalCount, int FiveStarCount, int FourStarCount, int ThreeStarCount, int TwoStarCount, int OneStarCount)> GetSummaryAsync(Guid profileId, CancellationToken ct = default);
    Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken ct = default);
    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetAllForAdminAsync(int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Review review, CancellationToken ct = default);
    Task<int> NewSerialNumberAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
