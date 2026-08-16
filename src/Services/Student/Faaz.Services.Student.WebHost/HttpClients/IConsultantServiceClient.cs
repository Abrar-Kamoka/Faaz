namespace Faaz.Services.Student.WebHost.HttpClients;

public record SavedConsultantSessionType(Guid Id, string Name, int DurationMinutes, decimal PriceGbp);

public record SavedConsultantSummary(
    Guid UserId,
    Guid ProfileId,
    string DisplayName,
    string? ProfessionalPhotoUrl,
    string CurrentRole,
    string Institution,
    bool IsVerified,
    decimal AverageRating,
    int ReviewCount,
    string[] SubjectAreas,
    SavedConsultantSessionType[] SessionTypes,
    bool IsAvailableThisWeek);

public interface IConsultantServiceClient
{
    // Returns null if the consultant profile no longer exists (deleted/deactivated) — callers should
    // skip that saved entry rather than fail the whole list.
    Task<SavedConsultantSummary?> GetProfileSummaryAsync(Guid consultantUserId, CancellationToken ct = default);
}
