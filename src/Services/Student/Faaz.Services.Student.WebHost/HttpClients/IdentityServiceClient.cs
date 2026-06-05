using System.Net.Http.Json;

namespace Faaz.Services.Student.WebHost.HttpClients;

internal sealed class IdentityServiceClient(HttpClient http) : IIdentityServiceClient
{
    public async Task UpdateUserNameAsync(Guid userId, string firstName, string lastName, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync(
            $"/api/v1/users/internal/{userId}/name",
            new { firstName, lastName },
            ct);
        response.EnsureSuccessStatusCode();
    }
}
