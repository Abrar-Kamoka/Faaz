using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Faaz.Services.Student.WebHost.HttpClients;

public record ValidateReferenceIdsResponse(Guid[] InvalidUniversityIds, Guid[] InvalidProgrammeIds, Guid[] InvalidSubjectIds, Guid[] InvalidServiceIds);

public interface IAdministrationReferenceClient
{
    // Null return means the Administration service couldn't be reached / didn't respond
    // successfully — callers must treat that as "could not validate", not as "everything is valid".
    Task<ValidateReferenceIdsResponse?> ValidateAsync(
        Guid[]? universityIds, Guid[]? programmeIds, Guid[]? subjectIds, Guid[]? serviceIds, CancellationToken ct = default);
}

file record FaazResp<T>(bool Success, T? Data);

internal sealed class AdministrationReferenceClient : IAdministrationReferenceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly string _serviceKey;

    public AdministrationReferenceClient(HttpClient http, IConfiguration config)
    {
        _http       = http;
        _serviceKey = config["ServiceApiKey"] ?? "dev-service-key";
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
        if (!resp.IsSuccessStatusCode) return null;

        var wrapper = await resp.Content.ReadFromJsonAsync<FaazResp<ValidateReferenceIdsResponse>>(JsonOptions, ct);
        return wrapper is { Success: true } ? wrapper.Data : null;
    }
}
