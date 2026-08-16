using Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;
using Faaz.SharedKernel.Exceptions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Identity.WebHost.HttpClients;

internal sealed class ConsultantServiceClient : IConsultantServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ConsultantServiceClient> _logger;

    public ConsultantServiceClient(HttpClient http, ILogger<ConsultantServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<Guid> InitialiseApplicationAsync(ApplicationSubmissionRequest request, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();

        AddField(content, "Email",            request.Email);
        AddField(content, "FirstName",        request.FirstName);
        AddField(content, "LastName",         request.LastName);
        AddField(content, "PhoneNumber",      request.PhoneNumber);
        AddField(content, "IsUkBased",        request.IsUkBased.ToString());
        AddField(content, "CurrentRole",      request.CurrentRole);
        AddField(content, "ExpertiseArea",    request.ExpertiseArea);
        AddField(content, "YearsOfExperience", request.YearsOfExperience.ToString());

        if (request.Institution is not null)
            AddField(content, "Institution", request.Institution);
        if (request.DateOfBirth.HasValue)
            AddField(content, "DateOfBirth", request.DateOfBirth.Value.ToString("yyyy-MM-dd"));
        if (request.Nationality is not null)
            AddField(content, "Nationality", request.Nationality);
        if (request.CountryOfResidence is not null)
            AddField(content, "CountryOfResidence", request.CountryOfResidence);
        if (request.LinkedInProfileUrl is not null)
            AddField(content, "LinkedInProfileUrl", request.LinkedInProfileUrl);
        if (request.HighestQualification.HasValue)
            AddField(content, "HighestQualification", ((int)request.HighestQualification.Value).ToString());
        if (request.PrimaryLanguage is not null)
            AddField(content, "PrimaryLanguage", request.PrimaryLanguage);
        if (request.PersonalStatement is not null)
            AddField(content, "PersonalStatement", request.PersonalStatement);
        if (request.ConsultationMode.HasValue)
            AddField(content, "ConsultationMode", ((int)request.ConsultationMode.Value).ToString());
        if (request.ReferralSource is not null)
            AddField(content, "ReferralSource", request.ReferralSource);

        if (request.Documents is { Count: > 0 })
        {
            foreach (var doc in request.Documents)
            {
                var stream = doc.File.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(doc.File.ContentType);
                content.Add(fileContent, "Files", doc.File.FileName);
                AddField(content, "DocumentTypes", ((int)doc.DocumentType).ToString());
            }
        }

        var response = await _http.PostAsync("/api/v1/consultants/internal/initialise-application", content, ct);
        await ThrowIfConflictAsync(response, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiWrapper<ApplicationIdResponse>>(cancellationToken: ct);
        return result!.Data!.ApplicationId;
    }

    public async Task<bool> GetProfileIsCompleteAsync(Guid userId, CancellationToken ct)
    {
        var response = await _http.GetAsync($"/api/v1/consultant-profiles/internal/{userId}/is-complete", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return false;
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IsCompleteResponse>(cancellationToken: ct);
        return result?.IsComplete ?? false;
    }

    public async Task NotifyEmailVerifiedAsync(Guid userId, CancellationToken ct)
    {
        var r1 = await _http.PostAsync($"/api/v1/consultants/internal/applications/user/{userId}/set-under-review",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"), ct);
        if (r1.StatusCode != HttpStatusCode.NotFound) r1.EnsureSuccessStatusCode();

        var r2 = await _http.PostAsJsonAsync("/api/v1/consultant-profiles/internal/create-stub", new { userId }, ct);
        r2.EnsureSuccessStatusCode();
    }

    public async Task LinkUserToApplicationAsync(Guid applicationId, Guid userId, CancellationToken ct)
    {
        var payload = new { applicationId, userId };
        var response = await _http.PostAsJsonAsync("/api/v1/consultants/internal/link-user", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<(string email, Guid applicationId)> ValidateInviteTokenAsync(string token, CancellationToken ct)
    {
        var response = await _http.GetAsync($"/api/v1/consultants/internal/validate-invite?token={Uri.EscapeDataString(token)}", ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiWrapper<InviteValidationResponse>>(cancellationToken: ct);
        return (result!.Data!.Email, result.Data.ApplicationId);
    }

    public async Task<PagedResult<ApplicationSummaryDto>> GetApplicationsAsync(string? status, int page, int pageSize, CancellationToken ct)
    {
        var url = $"/api/v1/consultants/internal/applications?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<ApplicationSummaryDto>>(cancellationToken: ct))!;
    }

    public async Task<ApplicationDetailDto> GetApplicationDetailAsync(Guid applicationId, CancellationToken ct)
    {
        var response = await _http.GetAsync($"/api/v1/consultants/internal/applications/{applicationId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApplicationDetailDto>(cancellationToken: ct))!;
    }

    public async Task<string> PreApproveApplicationAsync(Guid applicationId, string? notes, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync($"/api/v1/consultants/internal/applications/{applicationId}/pre-approve", new { notes }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiWrapper<PreApproveData>>(cancellationToken: ct);
        return result!.Data!.InviteToken;
    }

    public async Task ApproveApplicationAsync(Guid applicationId, string? notes, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync($"/api/v1/consultants/internal/applications/{applicationId}/approve", new { notes }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectApplicationAsync(Guid applicationId, string reason, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync($"/api/v1/consultants/internal/applications/{applicationId}/reject", new { reason }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RequestRevisionAsync(Guid applicationId, string notes, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync($"/api/v1/consultants/internal/applications/{applicationId}/request-revision", new { notes }, ct);
        response.EnsureSuccessStatusCode();
    }

    private static void AddField(MultipartFormDataContent content, string name, string value)
        => content.Add(new StringContent(value), name);

    private static async Task ThrowIfConflictAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode != HttpStatusCode.Conflict) return;
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(cancellationToken: ct);
        throw new ConflictException(body?.Message ?? "A duplicate record already exists.");
    }

    private sealed record ErrorEnvelope(string? Message);
    private sealed record ApplicationIdResponse(Guid ApplicationId);
    private sealed record IsCompleteResponse(bool IsComplete);
    private sealed record InviteValidationResponse(string Email, Guid ApplicationId);
    private sealed record ApiWrapper<T>(T? Data);
    private sealed record PreApproveData(string InviteToken);
}
