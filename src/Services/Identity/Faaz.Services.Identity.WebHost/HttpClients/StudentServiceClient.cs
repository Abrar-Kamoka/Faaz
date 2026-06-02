using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Identity.WebHost.HttpClients;

internal sealed class StudentServiceClient : IStudentServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<StudentServiceClient> _logger;

    public StudentServiceClient(HttpClient http, ILogger<StudentServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task CreateProfileStubAsync(Guid userId, string email, string firstName, string lastName, CancellationToken ct)
    {
        var payload = new { userId, email, firstName, lastName };
        var response = await _http.PostAsJsonAsync("/api/v1/students/internal/create-profile-stub", payload, ct);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Created student profile stub for {UserId}", userId);
    }
}
