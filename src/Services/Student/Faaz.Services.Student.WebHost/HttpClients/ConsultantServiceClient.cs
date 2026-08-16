using System.Net.Http.Json;
using System.Text.Json;
using Faaz.SharedKernel.Results;

namespace Faaz.Services.Student.WebHost.HttpClients;

internal sealed class ConsultantServiceClient(HttpClient http) : IConsultantServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Wire shape of the public GET /consultant-profiles/{userId} response's `data` — only the
    // fields SavedConsultantSummary actually needs.
    private record WireSessionType(Guid Id, string Name, int DurationMinutes, decimal PriceGbp);
    private record WireProfile(
        Guid Id, Guid UserId, string DisplayName, string? ProfessionalPhotoUrl,
        string CurrentRole, string Institution, bool IsVerified, decimal AverageRating,
        int ReviewCount, string[] SubjectAreas, List<WireSessionType> SessionTypes, bool IsAvailableThisWeek);

    public async Task<SavedConsultantSummary?> GetProfileSummaryAsync(Guid consultantUserId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/api/v1/consultant-profiles/{consultantUserId}", ct);
        if (!response.IsSuccessStatusCode) return null;

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<WireProfile>>(JsonOptions, ct);
        if (envelope?.Data is not { } p) return null;

        return new SavedConsultantSummary(
            p.UserId, p.Id, p.DisplayName, p.ProfessionalPhotoUrl, p.CurrentRole, p.Institution,
            p.IsVerified, p.AverageRating, p.ReviewCount, p.SubjectAreas ?? [],
            (p.SessionTypes ?? []).Select(s => new SavedConsultantSessionType(s.Id, s.Name, s.DurationMinutes, s.PriceGbp)).ToArray(),
            p.IsAvailableThisWeek);
    }
}
