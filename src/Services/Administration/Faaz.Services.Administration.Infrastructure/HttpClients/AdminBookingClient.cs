using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Faaz.Services.Administration.Infrastructure.HttpClients;

public interface IAdminBookingClient
{
    Task<PagedBookings?> GetBookingsAsync(int page, int pageSize, int? status, Guid? consultantId, Guid? studentId, CancellationToken ct = default);
    Task<AdminBookingDetail?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<BookingAnalytics?> GetAnalyticsAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<int> GetStudentBookingCountAsync(Guid studentId, CancellationToken ct = default);
}

public record AdminBookingDetail(
    Guid Id, string Reference, Guid StudentUserId, string StudentName,
    Guid ConsultantUserId, string ConsultantName, string SessionTypeName,
    int Status, decimal AmountGbp, decimal PlatformFeeGbp,
    DateTime ScheduledStartUtc, DateTime ScheduledEndUtc, DateTime CreatedAt,
    string? DisputeReason = null, string? DisputeResolution = null,
    string? DisputeResolutionNote = null, DateTime? DisputeResolvedAt = null);

public record PagedBookings(List<AdminBookingDetail> Items, int TotalCount);
public record StudentBookingSummary(int TotalBookings);

public record BookingAnalytics(
    int TotalBookings, int CompletedBookings, int CancelledBookings,
    int DisputedBookings, decimal TotalRevenueGbp, decimal PlatformRevenueGbp,
    int ActiveSessions);

file record FaazResp<T>(bool Success, T? Data);

internal sealed class AdminBookingClient : IAdminBookingClient
{
    private readonly HttpClient _http;
    private readonly string _serviceKey;
    private readonly ILogger<AdminBookingClient> _logger;

    public AdminBookingClient(HttpClient http, IConfiguration config, ILogger<AdminBookingClient> logger)
    {
        _http       = http;
        _serviceKey = config["ServiceApiKey"] ?? "dev-service-key";
        _logger     = logger;
    }

    public async Task<PagedBookings?> GetBookingsAsync(int page, int pageSize, int? status, Guid? consultantId, Guid? studentId, CancellationToken ct = default)
    {
        var qs = $"?page={page}&pageSize={pageSize}";
        if (status.HasValue)       qs += $"&status={status.Value}";
        if (consultantId.HasValue) qs += $"&consultantId={consultantId.Value}";
        if (studentId.HasValue)    qs += $"&studentId={studentId.Value}";
        return await GetAsync<PagedBookings>($"/internal/admin/bookings{qs}", ct);
    }

    public async Task<AdminBookingDetail?> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
        => await GetAsync<AdminBookingDetail>($"/internal/admin/bookings/{bookingId}", ct);

    public async Task<BookingAnalytics?> GetAnalyticsAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var qs = "";
        if (from.HasValue) qs += $"?from={from.Value:O}";
        if (to.HasValue)   qs += (qs.Length > 0 ? "&" : "?") + $"to={to.Value:O}";
        return await GetAsync<BookingAnalytics>($"/internal/admin/analytics{qs}", ct);
    }

    public async Task<int> GetStudentBookingCountAsync(Guid studentId, CancellationToken ct = default)
    {
        var result = await GetAsync<StudentBookingSummary>($"/internal/admin/students/{studentId}/summary", ct);
        return result?.TotalBookings ?? 0;
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) { _logger.LogWarning("Booking GET {Url} → {Status}", url, resp.StatusCode); return default; }
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<T>>(cancellationToken: ct);
        return wrapper is { Success: true } ? wrapper.Data : default;
    }
}
