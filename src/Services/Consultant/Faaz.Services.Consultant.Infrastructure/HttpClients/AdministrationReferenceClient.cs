using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Faaz.Services.Consultant.Infrastructure.HttpClients;

public interface IAdministrationReferenceClient
{
    // Null return means the Administration service couldn't be reached / didn't respond
    // successfully — callers must treat that as "could not validate", not as "everything is valid".
    Task<ValidateReferenceIdsResponse?> ValidateAsync(
        Guid[]? universityIds, Guid[]? programmeIds, Guid[]? subjectIds, Guid[]? serviceIds, CancellationToken ct = default);
}

public record ValidateReferenceIdsResponse(Guid[] InvalidUniversityIds, Guid[] InvalidProgrammeIds, Guid[] InvalidSubjectIds, Guid[] InvalidServiceIds);

file record FaazResp<T>(bool Success, T? Data);

internal sealed class AdministrationReferenceClient : IAdministrationReferenceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly string _serviceKey;
    private readonly ILogger<AdministrationReferenceClient> _logger;

    public AdministrationReferenceClient(HttpClient http, IConfiguration config, ILogger<AdministrationReferenceClient> logger)
    {
        _http       = http;
        _serviceKey = config["ServiceApiKey"] ?? "dev-service-key";
        _logger     = logger;
    }

    public async Task<ValidateReferenceIdsResponse?> ValidateAsync(
        Guid[]? universityIds, Guid[]? programmeIds, Guid[]? subjectIds, Guid[]? serviceIds, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/internal/reference/validate")
        {
            Content = JsonContent.Create(new { universityIds, programmeIds, subjectIds, serviceIds })
        };
        req.Headers.Add("X-Service-Key", _serviceKey);

        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Administration POST /internal/reference/validate → {Status}", resp.StatusCode);
            return null;
        }

        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<ValidateReferenceIdsResponse>>(JsonOptions, ct);
        return wrapper is { Success: true } ? wrapper.Data : null;
    }
}
