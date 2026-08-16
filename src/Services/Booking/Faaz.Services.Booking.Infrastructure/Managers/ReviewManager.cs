using Faaz.Services.Booking.Domain.Entities;
using Faaz.Services.Booking.Infrastructure.DatabaseContext;
using Faaz.Services.Booking.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Faaz.Services.Booking.Domain.BookingEnums;

namespace Faaz.Services.Booking.Infrastructure.Managers;

internal sealed class ReviewManager : IReviewServices
{
    private readonly BookingDbContext _db;

    public ReviewManager(BookingDbContext db)
    {
        _db = db;
    }

    public async Task<Review?> GetByIdAsync(Guid reviewId, CancellationToken ct = default)
    {
        return await _db.Reviews.FirstOrDefaultAsync(x => x.Id == reviewId, ct);
    }

    public async Task<Review?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        return await _db.Reviews.FirstOrDefaultAsync(x => x.BookingId == bookingId, ct);
    }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByConsultantProfileIdAsync(
        Guid profileId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Reviews
            .Where(x => x.ConsultantProfileId == profileId && x.IsPublic)
            .OrderByDescending(x => x.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(decimal AverageRating, int TotalCount, int FiveStarCount, int FourStarCount, int ThreeStarCount, int TwoStarCount, int OneStarCount)> GetSummaryAsync(
        Guid profileId, CancellationToken ct = default)
    {
        var reviews = await _db.Reviews
            .Where(x => x.ConsultantProfileId == profileId && x.IsPublic)
            .Select(x => x.Rating)
            .ToListAsync(ct);

        if (reviews.Count == 0)
            return (0m, 0, 0, 0, 0, 0, 0);

        return (
            reviews.Average(r => (decimal)r),
            reviews.Count,
            reviews.Count(r => r == ReviewRating.Five),
            reviews.Count(r => r == ReviewRating.Four),
            reviews.Count(r => r == ReviewRating.Three),
            reviews.Count(r => r == ReviewRating.Two),
            reviews.Count(r => r == ReviewRating.One)
        );
    }

    public async Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken ct = default)
    {
        return await _db.Reviews.AnyAsync(x => x.BookingId == bookingId, ct);
    }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetAllForAdminAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Reviews
            .IgnoreQueryFilters()
            .Include(r => r.Booking)
            .OrderByDescending(r => r.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(Review review, CancellationToken ct = default)
    {
        await _db.Reviews.AddAsync(review, ct);
    }

    public async Task<int> NewSerialNumberAsync(CancellationToken ct = default)
    {
        var max = await _db.Reviews.MaxAsync(x => (int?)x.SrNo, ct);
        return (max ?? 0) + 1;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}

