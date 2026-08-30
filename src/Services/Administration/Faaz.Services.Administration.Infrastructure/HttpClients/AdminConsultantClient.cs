using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Faaz.Services.Administration.Infrastructure.HttpClients;

public interface IAdminConsultantClient
{
    Task<PagedApplications?> GetApplicationsAsync(int page, int pageSize, int? status, CancellationToken ct = default);
    Task<AdminApplicationDetail?> GetApplicationByIdAsync(Guid applicationId, CancellationToken ct = default);
    Task<bool> ApproveApplicationAsync(Guid applicationId, Guid adminId, CancellationToken ct = default);
    Task<bool> RejectApplicationAsync(Guid applicationId, Guid adminId, string reason, CancellationToken ct = default);
    Task<bool> RequestRevisionAsync(Guid applicationId, Guid adminId, string notes, CancellationToken ct = default);
    Task<PagedProfiles?> GetProfilesAsync(int page, int pageSize, CancellationToken ct = default);
    Task<AdminProfileDetail?> GetProfileByIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> SuspendConsultantAsync(Guid userId, Guid adminId, string reason, CancellationToken ct = default);
    Task<bool> RestoreConsultantAsync(Guid userId, Guid adminId, CancellationToken ct = default);
    Task<PagedProfiles?> GetFeaturedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<bool> FeatureConsultantAsync(Guid userId, Guid adminId, CancellationToken ct = default);
    Task<bool> UnfeatureConsultantAsync(Guid userId, Guid adminId, CancellationToken ct = default);
}

public record ApplicationDocumentDto(Guid Id, string DocumentType, string FileName, string Url, DateTime UploadedAt);

// Field names must match the Consultant service's actual JSON exactly (case-insensitive matching
// covers casing, but not name mismatches) — a prior version of this record used unrelated names
// (UniversityName/SubjectName/LinkedInUrl) that matched nothing in the real response, so every one
// of these silently deserialized to null/default and the admin UI had almost nothing to show.
public record AdminApplicationDetail(
    Guid Id, Guid? UserId, string Email, string FirstName, string LastName,
    string? PhoneNumber, bool IsUkBased, string? CurrentRole,
    string? ExpertiseArea, int YearsOfExperience,
    DateOnly? DateOfBirth, string? Nationality, string? CountryOfResidence,
    string? LinkedInProfileUrl, string? HighestQualification, string? PrimaryLanguage,
    string? PersonalStatement, string? ConsultationMode,
    int ApplicationStatus, string? AdminNotes, DateTime? SetupInviteSentAt, DateTime SubmittedAt,
    List<ApplicationDocumentDto>? Documents);

public record PagedApplications(List<AdminApplicationDetail> Items, int TotalCount);

// Field names must match the Consultant service's actual JSON exactly (case-insensitive matching
// covers casing, but not name mismatches) — this previously used Bio/ProfilePhotoUrl, which match
// nothing in the real response (WrittenBio/ProfessionalPhotoUrl), so both silently deserialized to
// null and the admin UI never showed a photo or bio despite the data existing. See the identical
// warning already on AdminApplicationDetail above.
public record AdminProfileDetail(
    Guid UserId, string FullLegalName, string? WrittenBio, string? ProfessionalPhotoUrl,
    string Email, int ApplicationStatus, bool IsActive, decimal HourlyRateGbp,
    string? CurrentRole = null, string? Institution = null, int? YearsOfExperience = null,
    Guid[]? SubjectIds = null, List<AdminSessionTypeDto>? SessionTypes = null);

public record AdminSessionTypeDto(string Name, int DurationMinutes, decimal PriceGbp);

public record PagedProfiles(List<AdminProfileDetail> Items, int TotalCount);

file record FaazResp<T>(bool Success, T? Data);

internal sealed class AdminConsultantClient : IAdminConsultantClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly string _serviceKey;
    private readonly ILogger<AdminConsultantClient> _logger;

    public AdminConsultantClient(HttpClient http, IConfiguration config, ILogger<AdminConsultantClient> logger)
    {
        _http       = http;
        _serviceKey = config["ServiceApiKey"] ?? "dev-service-key";
        _logger     = logger;
    }

    public async Task<PagedApplications?> GetApplicationsAsync(int page, int pageSize, int? status, CancellationToken ct = default)
    {
        var qs = $"?page={page}&pageSize={pageSize}";
        if (status.HasValue) qs += $"&status={status.Value}";
        return await GetAsync<PagedApplications>($"/internal/admin/consultants/applications{qs}", ct);
    }

    public async Task<AdminApplicationDetail?> GetApplicationByIdAsync(Guid applicationId, CancellationToken ct = default)
        => await GetAsync<AdminApplicationDetail>($"/internal/admin/consultants/applications/{applicationId}", ct);

    public async Task<bool> ApproveApplicationAsync(Guid applicationId, Guid adminId, CancellationToken ct = default)
        => await PostAsync($"/internal/admin/consultants/applications/{applicationId}/approve", new { adminId }, ct);

    public async Task<bool> RejectApplicationAsync(Guid applicationId, Guid adminId, string reason, CancellationToken ct = default)
        => await PostAsync($"/internal/admin/consultants/applications/{applicationId}/reject", new { adminId, reason }, ct);

    public async Task<bool> RequestRevisionAsync(Guid applicationId, Guid adminId, string notes, CancellationToken ct = default)
        => await PostAsync($"/internal/admin/consultants/applications/{applicationId}/revision", new { adminId, notes }, ct);

    public async Task<PagedProfiles?> GetProfilesAsync(int page, int pageSize, CancellationToken ct = default)
        => await GetAsync<PagedProfiles>($"/internal/admin/consultants/profiles?page={page}&pageSize={pageSize}", ct);

    public async Task<AdminProfileDetail?> GetProfileByIdAsync(Guid userId, CancellationToken ct = default)
        => await GetAsync<AdminProfileDetail>($"/internal/admin/consultants/profiles/{userId}", ct);

    public async Task<bool> SuspendConsultantAsync(Guid userId, Guid adminId, string reason, CancellationToken ct = default)
        => await PostAsync($"/internal/admin/consultants/{userId}/suspend", new { adminId, reason }, ct);

    public async Task<bool> RestoreConsultantAsync(Guid userId, Guid adminId, CancellationToken ct = default)
        => await PostAsync($"/internal/admin/consultants/{userId}/restore", new { adminId }, ct);

    public async Task<PagedProfiles?> GetFeaturedAsync(int page, int pageSize, CancellationToken ct = default)
        => await GetAsync<PagedProfiles>($"/internal/admin/consultants/featured?page={page}&pageSize={pageSize}", ct);

    public async Task<bool> FeatureConsultantAsync(Guid userId, Guid adminId, CancellationToken ct = default)
        => await PostAsync($"/internal/admin/consultants/{userId}/feature", new { adminId }, ct);

    public async Task<bool> UnfeatureConsultantAsync(Guid userId, Guid adminId, CancellationToken ct = default)
        => await PostAsync($"/internal/admin/consultants/{userId}/unfeature", new { adminId }, ct);

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) { _logger.LogWarning("Consultant GET {Url} → {Status}", url, resp.StatusCode); return default; }
        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<T>>(JsonOptions, ct);
        return wrapper is { Success: true } ? wrapper.Data : default;
    }

    private async Task<bool> PostAsync(string url, object body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        req.Headers.Add("X-Service-Key", _serviceKey);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) _logger.LogWarning("Consultant POST {Url} → {Status}", url, resp.StatusCode);
        return resp.IsSuccessStatusCode;
    }
}
