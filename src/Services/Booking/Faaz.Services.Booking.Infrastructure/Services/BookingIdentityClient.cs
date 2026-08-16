using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Faaz.Services.Booking.Infrastructure.Services;

public interface IBookingIdentityClient
{
    Task<UserNameResult?> GetUserNameAsync(Guid userId, CancellationToken ct = default);
}

public record UserNameResult(string FirstName, string LastName)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

file record FaazResp<T>(bool Success, T? Data);

internal sealed class BookingIdentityClient : IBookingIdentityClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly string _serviceKey;
    private readonly ILogger<BookingIdentityClient> _logger;

    public BookingIdentityClient(HttpClient http, IConfiguration config, ILogger<BookingIdentityClient> logger)
    {
        _http       = http;
        _serviceKey = config["ServiceApiKey"] ?? "dev-service-key";
        _logger     = logger;
    }

    // Reuses Identity's existing internal admin-users lookup (X-Service-Key gated, not
    // Administration-service-specific) — no dedicated "get name" route exists, and this is the
    // same pattern Administration's AdminIdentityClient already uses for the identical call.
    public async Task<UserNameResult?> GetUserNameAsync(Guid userId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/internal/admin/users/{userId}");
        request.Headers.Add("X-Service-Key", _serviceKey);

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("User name lookup returned {Status} for user {UserId}", response.StatusCode, userId);
            return null;
        }

        var wrapper = await response.Content.ReadFromJsonAsync<FaazResp<UserNameResult>>(JsonOptions, ct);
        return wrapper is { Success: true } ? wrapper.Data : null;
    }
}
