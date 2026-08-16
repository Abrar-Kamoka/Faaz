using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Faaz.Services.Administration.Infrastructure.HttpClients;

public interface IAdminNotificationClient
{
    Task<List<AdminNotificationTemplate>?> GetTemplatesAsync(CancellationToken ct = default);
    Task<bool> UpdateTemplateAsync(Guid templateId, string subject, string body, CancellationToken ct = default);
    Task<List<AdminAnnouncement>?> GetAnnouncementsAsync(CancellationToken ct = default);
    Task<Guid?> CreateAnnouncementAsync(string title, string body, int audience, DateTime? expiresAt, Guid adminId, CancellationToken ct = default);
    Task<bool> DeactivateAnnouncementAsync(Guid id, CancellationToken ct = default);
}

public record AdminNotificationTemplate(Guid Id, string Key, string Channel, string Subject, string Body, string Description, DateTime? UpdatedAt);
public record AdminAnnouncement(Guid Id, string Title, string Body, int Audience, bool IsActive, DateTime? PublishedAt, DateTime? ExpiresAt, Guid CreatedByAdminId);

file record FaazResp<T>(bool Success, T? Data);

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

    public async Task<Guid?> CreateAnnouncementAsync(string title, string body, int audience, DateTime? expiresAt, Guid adminId, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/internal/admin/announcements")
        {
            Content = JsonContent.Create(new { title, body, audience, expiresAt, adminId })
        };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) { _logger.LogWarning("Notification POST announcements → {Status}", resp.StatusCode); return null; }
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<CreatedAnnouncementId>>(JsonOptions, ct);
        return wrapper is { Success: true } ? wrapper.Data?.Id : null;
    }

    public async Task<bool> DeactivateAnnouncementAsync(Guid id, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/internal/admin/announcements/{id}/deactivate");
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) _logger.LogWarning("Notification POST announcements/{Id}/deactivate → {Status}", id, resp.StatusCode);
        return resp.IsSuccessStatusCode;
    }
}

file record CreatedAnnouncementId(Guid Id);
