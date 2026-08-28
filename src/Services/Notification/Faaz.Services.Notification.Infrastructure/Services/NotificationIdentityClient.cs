using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Faaz.Services.Notification.Infrastructure.Services;

public interface INotificationIdentityClient
{
    Task<UserContactInfo?> GetUserAsync(Guid userId, CancellationToken ct = default);
}

public record UserContactInfo(string Email, string FirstName, string LastName)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

file record FaazResp<T>(bool Success, T? Data);

// Every booking/session integration event carries the participants' user IDs but not their contact
// details — resolving email here (rather than threading Email/FirstName through every event contract
// and every publish call site in Booking) keeps this a one-file addition instead of touching a dozen
// files across two services. Reuses Identity's existing internal admin-users lookup, same pattern as
// Administration's AdminIdentityClient and Booking's BookingIdentityClient.
internal sealed class NotificationIdentityClient : INotificationIdentityClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly string _serviceKey;
    private readonly ILogger<NotificationIdentityClient> _logger;

    public NotificationIdentityClient(HttpClient http, IConfiguration config, ILogger<NotificationIdentityClient> logger)
    {
        _http       = http;
        _serviceKey = config["ServiceApiKey"] ?? "dev-service-key";
        _logger     = logger;
    }

    public async Task<UserContactInfo?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/internal/admin/users/{userId}");
        request.Headers.Add("X-Service-Key", _serviceKey);

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("User contact lookup returned {Status} for user {UserId}", response.StatusCode, userId);
            return null;
        }

        var wrapper = await response.Content.ReadFromJsonAsync<FaazResp<UserContactInfo>>(JsonOptions, ct);
        return wrapper is { Success: true } ? wrapper.Data : null;
    }
}
