using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Faaz.Services.Administration.Infrastructure.HttpClients;

public interface IAdminIdentityClient
{
    Task<PagedUsers?> GetUsersAsync(int page, int pageSize, string? search, string? role, bool? isActive, CancellationToken ct = default);
    Task<AdminUserDetail?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> DeactivateUserAsync(Guid userId, string reason, Guid adminId, CancellationToken ct = default);
    Task<bool> ReactivateUserAsync(Guid userId, Guid adminId, CancellationToken ct = default);
    Task<AdminUserDetail?> CreateStaffAsync(string email, string firstName, string lastName, string roleName, Guid createdByAdminId, CancellationToken ct = default);

    // Roles Management (RBAC) — the actual Role/Permission/RoleClaim rows live in Identity,
    // which already owns ASP.NET Core Identity's role infrastructure.
    Task<List<AdminPermission>?> GetPermissionsAsync(CancellationToken ct = default);
    Task<List<AdminRole>?> GetRolesAsync(CancellationToken ct = default);
    Task<Guid?> CreateRoleAsync(string name, string? description, List<string> permissionKeys, CancellationToken ct = default);
    Task<bool> UpdateRoleAsync(Guid roleId, string name, string? description, List<string> permissionKeys, CancellationToken ct = default);
    Task<(bool Success, string? Error)> DeleteRoleAsync(Guid roleId, CancellationToken ct = default);
    Task<bool> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);

    // Application decisions — proxied to Identity because approving/rejecting/pre-approving an
    // application must update the consultant's Identity user claims and publish the integration
    // events that Notification (email + SignalR pop) and Consultant (profile activation) consume.
    Task<ActionResult> PreApproveApplicationAsync(Guid applicationId, string email, string? notes, CancellationToken ct = default);
    Task<ActionResult> ApproveApplicationAsync(Guid applicationId, string email, string? notes, CancellationToken ct = default);
    Task<ActionResult> RejectApplicationAsync(Guid applicationId, string email, string reason, CancellationToken ct = default);
    Task<ActionResult> RequestRevisionApplicationAsync(Guid applicationId, string email, string notes, CancellationToken ct = default);
}

// ConsultantApplicationStatus mirrors Faaz.SharedKernel.SharedEnums.ConsultantApplicationStatus
// (Submitted=0, Invited=1, SettingUpProfile=2, PendingRevision=3, Active=4, Rejected=5, Suspended=6)
// — only populated for consultant rows; null for students/staff, where IsActive alone is accurate.
public record AdminUserDetail(Guid Id, string Email, string FirstName, string LastName, int Role, bool IsActive, DateTime CreatedAt, string? PhoneNumber, List<string>? RoleNames = null, int? ConsultantApplicationStatus = null);
public record PagedUsers(List<AdminUserDetail> Items, int TotalCount);
public record ActionResult(bool Success, string? Error);
public record AdminPermission(Guid Id, string Key, string Category, string Description);
public record AdminRole(Guid Id, string Name, string? Description, bool IsSystemRole, List<string> Permissions, int MemberCount);

file record FaazResp<T>(bool Success, T? Data);
file record FaazErrorResp(bool Success, string? Message);
file record CreatedRoleId(Guid Id);

internal sealed class AdminIdentityClient : IAdminIdentityClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly string _serviceKey;
    private readonly ILogger<AdminIdentityClient> _logger;

    public AdminIdentityClient(HttpClient http, IConfiguration config, ILogger<AdminIdentityClient> logger)
    {
        _http       = http;
        _serviceKey = config["ServiceApiKey"] ?? "dev-service-key";
        _logger     = logger;
    }

    public async Task<PagedUsers?> GetUsersAsync(int page, int pageSize, string? search, string? role, bool? isActive, CancellationToken ct = default)
    {
        var qs = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search))  qs += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrEmpty(role))    qs += $"&role={role}";
        if (isActive.HasValue)              qs += $"&isActive={isActive.Value}";

        return await GetAsync<PagedUsers>($"/internal/admin/users{qs}", ct);
    }

    public async Task<AdminUserDetail?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
        => await GetAsync<AdminUserDetail>($"/internal/admin/users/{userId}", ct);

    public async Task<bool> DeactivateUserAsync(Guid userId, string reason, Guid adminId, CancellationToken ct = default)
        => await PostAsync($"/internal/admin/users/{userId}/deactivate", new { reason, adminId }, ct);

    public async Task<bool> ReactivateUserAsync(Guid userId, Guid adminId, CancellationToken ct = default)
        => await PostAsync($"/internal/admin/users/{userId}/reactivate", new { adminId }, ct);

    public async Task<AdminUserDetail?> CreateStaffAsync(string email, string firstName, string lastName, string roleName, Guid createdByAdminId, CancellationToken ct = default)
        => await PostAndReadAsync<AdminUserDetail>("/internal/admin/staff", new { email, firstName, lastName, roleName, createdByAdminId }, ct);

    public async Task<ActionResult> PreApproveApplicationAsync(Guid applicationId, string email, string? notes, CancellationToken ct = default)
        => await PostActionAsync($"/internal/admin/applications/{applicationId}/pre-approve", new { email, notes }, ct);

    public async Task<ActionResult> ApproveApplicationAsync(Guid applicationId, string email, string? notes, CancellationToken ct = default)
        => await PostActionAsync($"/internal/admin/applications/{applicationId}/approve", new { email, notes }, ct);

    public async Task<ActionResult> RejectApplicationAsync(Guid applicationId, string email, string reason, CancellationToken ct = default)
        => await PostActionAsync($"/internal/admin/applications/{applicationId}/reject", new { email, notes = reason }, ct);

    public async Task<ActionResult> RequestRevisionApplicationAsync(Guid applicationId, string email, string notes, CancellationToken ct = default)
        => await PostActionAsync($"/internal/admin/applications/{applicationId}/request-revision", new { email, notes }, ct);

    public async Task<List<AdminPermission>?> GetPermissionsAsync(CancellationToken ct = default)
        => await GetAsync<List<AdminPermission>>("/internal/admin/permissions", ct);

    public async Task<List<AdminRole>?> GetRolesAsync(CancellationToken ct = default)
        => await GetAsync<List<AdminRole>>("/internal/admin/roles", ct);

    public async Task<Guid?> CreateRoleAsync(string name, string? description, List<string> permissionKeys, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/internal/admin/roles")
        {
            Content = JsonContent.Create(new { name, description, permissionKeys })
        };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) { _logger.LogWarning("Identity POST roles → {Status}", resp.StatusCode); return null; }
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<CreatedRoleId>>(JsonOptions, ct);
        return wrapper is { Success: true } ? wrapper.Data?.Id : null;
    }

    public async Task<bool> UpdateRoleAsync(Guid roleId, string name, string? description, List<string> permissionKeys, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/internal/admin/roles/{roleId}")
        {
            Content = JsonContent.Create(new { name, description, permissionKeys })
        };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) _logger.LogWarning("Identity PUT roles/{Id} → {Status}", roleId, resp.StatusCode);
        return resp.IsSuccessStatusCode;
    }

    public async Task<(bool Success, string? Error)> DeleteRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/internal/admin/roles/{roleId}");
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (resp.IsSuccessStatusCode) return (true, null);

        string? error = null;
        try
        {
            var wrapper = await resp.Content.ReadFromJsonAsync<FaazErrorResp>(JsonOptions, ct);
            error = wrapper?.Message;
        }
        catch (JsonException) { /* non-JSON error body — fall through to generic message */ }
        _logger.LogWarning("Identity DELETE roles/{Id} → {Status}: {Error}", roleId, resp.StatusCode, error);
        return (false, error ?? $"Request failed ({(int)resp.StatusCode}).");
    }

    public async Task<bool> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default)
        => await PostAsync($"/internal/admin/users/{userId}/assign-role", new { roleId }, ct);

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) { _logger.LogWarning("Identity GET {Url} → {Status}", url, resp.StatusCode); return default; }
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<T>>(JsonOptions, ct);
        return wrapper is { Success: true } ? wrapper.Data : default;
    }

    private async Task<bool> PostAsync(string url, object body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) _logger.LogWarning("Identity POST {Url} → {Status}", url, resp.StatusCode);
        return resp.IsSuccessStatusCode;
    }

    private async Task<T?> PostAndReadAsync<T>(string url, object body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) { _logger.LogWarning("Identity POST {Url} → {Status}", url, resp.StatusCode); return default; }
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<T>>(JsonOptions, ct);
        return wrapper is { Success: true } ? wrapper.Data : default;
    }

    // Unlike PostAsync, this surfaces the backend's actual validation message on failure (e.g.
    // "Send a setup invite first") instead of swallowing it into a generic bool — admin-application
    // decisions have real business-rule errors the admin needs to see, not just a pass/fail toast.
    private async Task<ActionResult> PostActionAsync(string url, object body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (resp.IsSuccessStatusCode) return new ActionResult(true, null);

        string? error = null;
        try
        {
            var wrapper = await resp.Content.ReadFromJsonAsync<FaazErrorResp>(JsonOptions, ct);
            error = wrapper?.Message;
        }
        catch (JsonException) { /* non-JSON error body — fall through to generic message */ }

        _logger.LogWarning("Identity POST {Url} → {Status}: {Error}", url, resp.StatusCode, error);
        return new ActionResult(false, error ?? $"Request failed ({(int)resp.StatusCode}).");
    }
}
