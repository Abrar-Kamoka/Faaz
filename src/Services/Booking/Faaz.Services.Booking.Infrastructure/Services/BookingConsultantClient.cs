using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Faaz.Services.Booking.Infrastructure.Services;

public interface IBookingConsultantClient
{
    Task<SlotCheckResult?> CheckSlotAvailabilityAsync(Guid consultantProfileId, Guid sessionTypeId, DateTime slotStartUtc, CancellationToken ct = default);
    Task<ConsultantScheduleResult?> GetConsultantScheduleAsync(Guid consultantProfileId, Guid sessionTypeId, CancellationToken ct = default);
}

public record SlotCheckResult(bool IsAvailable, Guid ConsultantUserId, string SessionTypeName, decimal SessionPriceGbp, int DurationMinutes);
public record ConsultantScheduleResult(
    List<WeeklySlotItem> WeeklySlots,
    List<string> BlockedDates,
    int DurationMinutes,
    int MinBookingNoticeHours,
    int MaxAdvanceBookingDays);
public record WeeklySlotItem(int DayOfWeek, string StartTimeUtc, string EndTimeUtc);

internal sealed class BookingConsultantClient : IBookingConsultantClient
{
    private readonly HttpClient _http;
    private readonly string _serviceKey;
    private readonly ILogger<BookingConsultantClient> _logger;

    public BookingConsultantClient(HttpClient http, IConfiguration config, ILogger<BookingConsultantClient> logger)
    {
        _http       = http;
        _serviceKey = config["ServiceApiKey"] ?? "dev-service-key";
        _logger     = logger;
    }

    public async Task<SlotCheckResult?> CheckSlotAvailabilityAsync(Guid consultantProfileId, Guid sessionTypeId, DateTime slotStartUtc, CancellationToken ct = default)
    {
        var url = $"/internal/consultant/slot-check?profileId={consultantProfileId}&sessionTypeId={sessionTypeId}&start={slotStartUtc:O}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Service-Key", _serviceKey);

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Slot check returned {Status} for profile {ProfileId}", response.StatusCode, consultantProfileId);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SlotCheckResult>(cancellationToken: ct);
    }

    public async Task<ConsultantScheduleResult?> GetConsultantScheduleAsync(Guid consultantProfileId, Guid sessionTypeId, CancellationToken ct = default)
    {
        var url = $"/internal/consultant/schedule?profileId={consultantProfileId}&sessionTypeId={sessionTypeId}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Service-Key", _serviceKey);

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Schedule fetch returned {Status} for profile {ProfileId}", response.StatusCode, consultantProfileId);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ConsultantScheduleResult>(cancellationToken: ct);
    }
}
