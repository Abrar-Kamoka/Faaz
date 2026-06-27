using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Faaz.Services.Payment.Infrastructure.Services;

internal sealed class BookingClient : IBookingClient
{
    private readonly HttpClient _http;
    private readonly string _serviceKey;
    private readonly ILogger<BookingClient> _logger;

    public BookingClient(HttpClient http, IConfiguration config, ILogger<BookingClient> logger)
    {
        _http       = http;
        _serviceKey = config["ServiceApiKey"] ?? "dev-service-key";
        _logger     = logger;
    }

    public async Task<BookingPaymentDetailsResult?> GetBookingDetailsAsync(Guid bookingId, CancellationToken ct = default)
    {
        var url = $"/internal/booking/{bookingId}/details";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Service-Key", _serviceKey);

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Booking details lookup returned {Status} for booking {BookingId}", response.StatusCode, bookingId);
            return null;
        }

        var wrapper = await response.Content.ReadFromJsonAsync<BookingDetailsWrapper>(cancellationToken: ct);
        return wrapper?.Data;
    }

    private record BookingDetailsWrapper(BookingPaymentDetailsResult? Data);
}
