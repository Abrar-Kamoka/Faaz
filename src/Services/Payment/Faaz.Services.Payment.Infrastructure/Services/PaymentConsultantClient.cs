using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Faaz.Services.Payment.Infrastructure.Services;

internal sealed class PaymentConsultantClient : IPaymentConsultantClient
{
    private readonly HttpClient _http;
    private readonly string _serviceKey;
    private readonly ILogger<PaymentConsultantClient> _logger;

    public PaymentConsultantClient(HttpClient http, IConfiguration config, ILogger<PaymentConsultantClient> logger)
    {
        _http       = http;
        _serviceKey = config["ServiceApiKey"] ?? "dev-service-key";
        _logger     = logger;
    }

    public async Task<string?> GetStripeConnectAccountIdAsync(Guid consultantUserId, CancellationToken ct = default)
    {
        var status = await GetStripeConnectStatusAsync(consultantUserId, ct);
        return status.AccountId;
    }

    public async Task<StripeConnectStatus> GetStripeConnectStatusAsync(Guid consultantUserId, CancellationToken ct = default)
    {
        var url = $"/internal/consultant/stripe-account?userId={consultantUserId}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Service-Key", _serviceKey);

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Stripe account lookup returned {Status} for user {UserId}", response.StatusCode, consultantUserId);
            return new StripeConnectStatus(null, false, false);
        }

        var result = await response.Content.ReadFromJsonAsync<StripeAccountResult>(cancellationToken: ct);
        return new StripeConnectStatus(result?.StripeConnectAccountId, result?.DetailsSubmitted ?? false, result?.ChargesEnabled ?? false);
    }

    public async Task SetStripeConnectAccountIdAsync(Guid consultantUserId, string stripeAccountId, CancellationToken ct = default)
    {
        var url = $"/internal/consultant/stripe-account?userId={consultantUserId}";
        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(new { StripeConnectAccountId = stripeAccountId })
        };
        request.Headers.Add("X-Service-Key", _serviceKey);
        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Stripe account save returned {Status} for user {UserId}", response.StatusCode, consultantUserId);
    }

    public async Task UpdateStripeConnectAccountStatusAsync(string stripeAccountId, bool detailsSubmitted, bool chargesEnabled, CancellationToken ct = default)
    {
        const string url = "/internal/consultant/stripe-status";
        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(new
            {
                StripeConnectAccountId = stripeAccountId,
                DetailsSubmitted       = detailsSubmitted,
                ChargesEnabled         = chargesEnabled
            })
        };
        request.Headers.Add("X-Service-Key", _serviceKey);
        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Stripe status sync returned {Status} for account {AccountId}", response.StatusCode, stripeAccountId);
    }

    private record StripeAccountResult(string? StripeConnectAccountId, bool DetailsSubmitted, bool ChargesEnabled);
}
