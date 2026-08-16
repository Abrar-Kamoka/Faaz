using Faaz.SharedKernel.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Faaz.Services.Administration.Infrastructure.HttpClients;

public interface IAdminPaymentClient
{
    Task<PagedTransactions?> GetTransactionsAsync(int page, int pageSize, string? type, DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<AdminTransactionDetail?> GetTransactionByIdAsync(Guid transactionId, CancellationToken ct = default);
    Task<PagedPayouts?> GetPayoutsAsync(int page, int pageSize, string? status, CancellationToken ct = default);
    Task<AdminPayoutDetail?> GetPayoutByIdAsync(Guid payoutId, CancellationToken ct = default);
    Task<bool> RefundTransactionAsync(Guid transactionId, Guid adminId, string reason, CancellationToken ct = default);
    Task<decimal> GetStudentTotalSpentAsync(Guid studentId, CancellationToken ct = default);
    Task<List<RevenueDay>?> GetRevenueTimeSeriesAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<TopConsultantEarning>?> GetTopConsultantsAsync(DateTime from, DateTime to, int take, CancellationToken ct = default);
    Task<PagedPromoCodes?> GetPromoCodesAsync(int page, int pageSize, CancellationToken ct = default);
    Task<AdminPromoCodeDetail?> GetPromoCodeByIdAsync(Guid id, CancellationToken ct = default);
    Task<AdminPromoCodeDetail?> CreatePromoCodeAsync(CreatePromoCodeBody body, CancellationToken ct = default);
    Task<bool> UpdatePromoCodeAsync(Guid id, UpdatePromoCodeBody body, CancellationToken ct = default);
    Task<bool> DeactivatePromoCodeAsync(Guid id, CancellationToken ct = default);
}

public record AdminTransactionDetail(
    Guid Id, Guid BookingId, string Reference, string Type,
    decimal AmountGbp, string Currency, string Status,
    string? StripePaymentIntentId, DateTime CreatedAt);

public record PagedTransactions(List<AdminTransactionDetail> Items, int TotalCount);
public record StudentSpendSummary(decimal TotalSpentGbp);
public record RevenueDay(DateTime Date, decimal RevenueGbp, decimal PlatformFeeGbp, int PaymentCount);
public record TopConsultantEarning(Guid ConsultantUserId, decimal TotalEarningsGbp, int BookingCount);

public record AdminPayoutDetail(
    Guid Id, Guid ConsultantUserId, string ConsultantName,
    decimal AmountGbp, string Status, string? StripeTransferId,
    DateTime CreatedAt, DateTime? ProcessedAt);

public record PagedPayouts(List<AdminPayoutDetail> Items, int TotalCount);

public record AdminPromoCodeDetail(
    Guid Id, string Code, string DiscountType, decimal DiscountValue,
    decimal? MaxDiscountAmount, int? MaxUses, int CurrentUses,
    DateTime? ValidFrom, DateTime? ValidTo, bool IsActive,
    string? Description, Guid? ConsultantProfileId);

public record PagedPromoCodes(List<AdminPromoCodeDetail> Items, int TotalCount);

public record CreatePromoCodeBody(
    string Code, string DiscountType, decimal DiscountValue,
    decimal? MaxDiscountAmount, int? MaxUses,
    DateTime? ValidFrom, DateTime? ValidTo,
    string? Description, Guid? ConsultantProfileId);

public record UpdatePromoCodeBody(
    decimal DiscountValue, decimal? MaxDiscountAmount, int? MaxUses,
    DateTime? ValidFrom, DateTime? ValidTo,
    string? Description, bool IsActive);

file record FaazResp<T>(bool Success, int StatusCode, string? Message, T? Data);

internal sealed class AdminPaymentClient : IAdminPaymentClient
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly string _serviceKey;
    private readonly ILogger<AdminPaymentClient> _logger;

    public AdminPaymentClient(HttpClient http, IConfiguration config, ILogger<AdminPaymentClient> logger)
    {
        _http       = http;
        _serviceKey = config["ServiceApiKey"] ?? "dev-service-key";
        _logger     = logger;
    }

    // Payment's own GlobalExceptionMiddleware already picked the right status/message (409 for a
    // duplicate code, 400 for bad input, etc.) — re-throwing the equivalent here instead of
    // collapsing everything to a generic 502 is what actually lets the admin see why it failed.
    private async Task<Exception> BuildDownstreamErrorAsync(HttpResponseMessage resp, string context, CancellationToken ct)
    {
        var body = await resp.Content.ReadFromJsonAsync<FaazResp<object>>(JsonOptions, ct);
        var message = body?.Message ?? $"{context} ({(int)resp.StatusCode} {resp.StatusCode}).";
        _logger.LogWarning("Payment call failed — {Context} → {Status}: {Message}", context, resp.StatusCode, message);

        return (int)resp.StatusCode switch
        {
            409 => new ConflictException(message),
            404 => new NotFoundException(message),
            _   => BusinessRuleException.Error(message, "admin.payment-call-failed")
        };
    }

    public async Task<PagedTransactions?> GetTransactionsAsync(int page, int pageSize, string? type, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var qs = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(type)) qs += $"&type={Uri.EscapeDataString(type)}";
        if (from.HasValue)               qs += $"&from={from.Value:O}";
        if (to.HasValue)                 qs += $"&to={to.Value:O}";
        return await GetAsync<PagedTransactions>($"/internal/admin/transactions{qs}", ct);
    }

    public async Task<AdminTransactionDetail?> GetTransactionByIdAsync(Guid transactionId, CancellationToken ct = default)
        => await GetAsync<AdminTransactionDetail>($"/internal/admin/transactions/{transactionId}", ct);

    public async Task<PagedPayouts?> GetPayoutsAsync(int page, int pageSize, string? status, CancellationToken ct = default)
    {
        var qs = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status)) qs += $"&status={Uri.EscapeDataString(status)}";
        return await GetAsync<PagedPayouts>($"/internal/admin/payouts{qs}", ct);
    }

    public async Task<AdminPayoutDetail?> GetPayoutByIdAsync(Guid payoutId, CancellationToken ct = default)
        => await GetAsync<AdminPayoutDetail>($"/internal/admin/payouts/{payoutId}", ct);

    public async Task<bool> RefundTransactionAsync(Guid transactionId, Guid adminId, string reason, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/internal/admin/transactions/{transactionId}/refund")
        {
            Content = JsonContent.Create(new { adminId, reason })
        };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, CancellationToken.None);
        if (!resp.IsSuccessStatusCode) _logger.LogWarning("Payment POST refund/{Id} → {Status}", transactionId, resp.StatusCode);
        return resp.IsSuccessStatusCode;
    }

    public async Task<PagedPromoCodes?> GetPromoCodesAsync(int page, int pageSize, CancellationToken ct = default)
        => await GetAsync<PagedPromoCodes>($"/internal/admin/promo-codes?page={page}&pageSize={pageSize}", ct);

    public async Task<AdminPromoCodeDetail?> GetPromoCodeByIdAsync(Guid id, CancellationToken ct = default)
        => await GetAsync<AdminPromoCodeDetail>($"/internal/admin/promo-codes/{id}", ct);

    public async Task<AdminPromoCodeDetail?> CreatePromoCodeAsync(CreatePromoCodeBody body, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/internal/admin/promo-codes")
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) throw await BuildDownstreamErrorAsync(resp, "Create promo code", ct);
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<AdminPromoCodeDetail>>(JsonOptions, ct);
        return wrapper is { Success: true } ? wrapper.Data : null;
    }

    public async Task<bool> UpdatePromoCodeAsync(Guid id, UpdatePromoCodeBody body, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/internal/admin/promo-codes/{id}")
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) _logger.LogWarning("Payment PUT promo-codes/{Id} → {Status}", id, resp.StatusCode);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeactivatePromoCodeAsync(Guid id, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/internal/admin/promo-codes/{id}/deactivate");
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) _logger.LogWarning("Payment POST promo-codes/{Id}/deactivate → {Status}", id, resp.StatusCode);
        return resp.IsSuccessStatusCode;
    }

    public async Task<decimal> GetStudentTotalSpentAsync(Guid studentId, CancellationToken ct = default)
    {
        var result = await GetAsync<StudentSpendSummary>($"/internal/admin/students/{studentId}/summary", ct);
        return result?.TotalSpentGbp ?? 0m;
    }

    public async Task<List<RevenueDay>?> GetRevenueTimeSeriesAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await GetAsync<List<RevenueDay>>($"/internal/admin/analytics/revenue-timeseries?from={from:O}&to={to:O}", ct);

    public async Task<List<TopConsultantEarning>?> GetTopConsultantsAsync(DateTime from, DateTime to, int take, CancellationToken ct = default)
        => await GetAsync<List<TopConsultantEarning>>($"/internal/admin/analytics/top-consultants?from={from:O}&to={to:O}&take={take}", ct);

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) { _logger.LogWarning("Payment GET {Url} → {Status}", url, resp.StatusCode); return default; }
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<T>>(cancellationToken: ct);
        return wrapper is { Success: true } ? wrapper.Data : default;
    }
}
