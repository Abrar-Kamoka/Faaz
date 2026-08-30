using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Faaz.Services.Consultant.Infrastructure.Services;

// Consultant doesn't own review data — best-effort live lookup against Booking's public,
// AllowAnonymous summary endpoint (no service-to-service auth needed). A lookup failure just
// leaves the profile's rating at its zero-review default rather than breaking the profile load.
internal sealed class BookingReviewClient : IBookingReviewClient
{
    private readonly HttpClient _http;
    private readonly ILogger<BookingReviewClient> _logger;

    public BookingReviewClient(HttpClient http, ILogger<BookingReviewClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ReviewSummaryResult?> GetReviewSummaryAsync(Guid consultantProfileId, CancellationToken ct = default)
    {
        try
        {
            var wrapper = await _http.GetFromJsonAsync<ReviewSummaryWrapper>(
                $"/api/reviews/consultant/{consultantProfileId}/summary", ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Review summary lookup failed for consultant profile {ProfileId}", consultantProfileId);
            return null;
        }
    }

    private record ReviewSummaryWrapper(ReviewSummaryResult? Data);
}
