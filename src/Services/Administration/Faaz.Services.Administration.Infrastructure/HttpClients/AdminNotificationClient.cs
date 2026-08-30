using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Faaz.Services.Administration.Infrastructure.HttpClients;

public interface IAdminNotificationClient
{
    Task<List<AdminNotificationTemplate>?> GetTemplatesAsync(CancellationToken ct = default);
    Task<bool> UpdateTemplateAsync(Guid templateId, string subject, string body, CancellationToken ct = default);
    Task<List<AdminAnnouncement>?> GetAnnouncementsAsync(CancellationToken ct = default);
    /// <returns>Success plus the announcement id, or failure with Notification's own validation
    /// message (e.g. "Expiry date must be in the future.") rather than a generic one.</returns>
    Task<AnnouncementActionResult<Guid>> CreateAnnouncementAsync(string title, string body, int audience, DateTime? expiresAt, Guid adminId, CancellationToken ct = default);
    Task<bool> DeactivateAnnouncementAsync(Guid id, CancellationToken ct = default);
    Task<AnnouncementActionResult> ActivateAnnouncementAsync(Guid id, CancellationToken ct = default);
    Task<AnnouncementActionResult> UpdateAnnouncementAsync(Guid id, string title, string body, int audience, DateTime? expiresAt, CancellationToken ct = default);
    Task<bool> DeleteAnnouncementAsync(Guid id, CancellationToken ct = default);
}

public record AdminNotificationTemplate(Guid Id, string Key, string Channel, string Subject, string Body, string Description, DateTime? UpdatedAt);
public record AdminAnnouncement(Guid Id, string Title, string Body, int Audience, bool IsActive, DateTime? PublishedAt, DateTime? ExpiresAt, Guid CreatedByAdminId);

public record AnnouncementActionResult(bool Success, string? ErrorMessage = null)
{
    public static readonly AnnouncementActionResult Ok = new(true);
}
public record AnnouncementActionResult<T>(bool Success, T? Data = default, string? ErrorMessage = null);

file record FaazResp<T>(bool Success, string Message, T? Data);

internal sealed class AdminNotificationClient : IAdminNotificationClient
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly string _serviceKey;
    private readonly ILogger<AdminNotificationClient> _logger;

    public AdminNotificationClient(HttpClient http, IConfiguration config, ILogger<AdminNotificationClient> logger)
    {
        _http       = http;
        _serviceKey = config["ServiceApiKey"] ?? "dev-service-key";
        _logger     = logger;
    }

    public async Task<List<AdminNotificationTemplate>?> GetTemplatesAsync(CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/internal/admin/templates");
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) { _logger.LogWarning("Notification GET templates → {Status}", resp.StatusCode); return null; }
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<List<AdminNotificationTemplate>>>(JsonOptions, ct);
        return wrapper is { Success: true } ? wrapper.Data : null;
    }

    public async Task<bool> UpdateTemplateAsync(Guid templateId, string subject, string body, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/internal/admin/templates/{templateId}")
        {
            Content = JsonContent.Create(new { subject, body })
        };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) _logger.LogWarning("Notification PUT templates/{Id} → {Status}", templateId, resp.StatusCode);
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<AdminAnnouncement>?> GetAnnouncementsAsync(CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/internal/admin/announcements");
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) { _logger.LogWarning("Notification GET announcements → {Status}", resp.StatusCode); return null; }
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<List<AdminAnnouncement>>>(JsonOptions, ct);
        return wrapper is { Success: true } ? wrapper.Data : null;
    }

    public async Task<AnnouncementActionResult<Guid>> CreateAnnouncementAsync(string title, string body, int audience, DateTime? expiresAt, Guid adminId, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/internal/admin/announcements")
        {
            Content = JsonContent.Create(new { title, body, audience, expiresAt, adminId })
        };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<CreatedAnnouncementId>>(JsonOptions, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Notification POST announcements → {Status}", resp.StatusCode);
            return new AnnouncementActionResult<Guid>(false, ErrorMessage: wrapper?.Message);
        }
        return new AnnouncementActionResult<Guid>(true, wrapper?.Data?.Id ?? Guid.Empty);
    }

    public async Task<bool> DeactivateAnnouncementAsync(Guid id, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/internal/admin/announcements/{id}/deactivate");
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) _logger.LogWarning("Notification POST announcements/{Id}/deactivate → {Status}", id, resp.StatusCode);
        return resp.IsSuccessStatusCode;
    }

    public async Task<AnnouncementActionResult> ActivateAnnouncementAsync(Guid id, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/internal/admin/announcements/{id}/activate");
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (resp.IsSuccessStatusCode) return AnnouncementActionResult.Ok;

        _logger.LogWarning("Notification POST announcements/{Id}/activate → {Status}", id, resp.StatusCode);
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<object?>>(JsonOptions, ct);
        return new AnnouncementActionResult(false, wrapper?.Message);
    }

    public async Task<AnnouncementActionResult> UpdateAnnouncementAsync(Guid id, string title, string body, int audience, DateTime? expiresAt, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/internal/admin/announcements/{id}")
        {
            Content = JsonContent.Create(new { title, body, audience, expiresAt })
        };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (resp.IsSuccessStatusCode) return AnnouncementActionResult.Ok;

        _logger.LogWarning("Notification PUT announcements/{Id} → {Status}", id, resp.StatusCode);
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<object?>>(JsonOptions, ct);
        return new AnnouncementActionResult(false, wrapper?.Message);
    }

    public async Task<bool> DeleteAnnouncementAsync(Guid id, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/internal/admin/announcements/{id}");
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) _logger.LogWarning("Notification DELETE announcements/{Id} → {Status}", id, resp.StatusCode);
        return resp.IsSuccessStatusCode;
    }
}

file record CreatedAnnouncementId(Guid Id);
