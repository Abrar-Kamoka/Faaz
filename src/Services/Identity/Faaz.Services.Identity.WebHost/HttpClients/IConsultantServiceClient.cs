using Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;

namespace Faaz.Services.Identity.WebHost.HttpClients;

public interface IConsultantServiceClient
{
    Task<Guid> InitialiseApplicationAsync(ApplicationSubmissionRequest request, CancellationToken ct = default);
    Task<bool> GetProfileIsCompleteAsync(Guid userId, CancellationToken ct = default);

    Task LinkUserToApplicationAsync(Guid applicationId, Guid userId, CancellationToken ct = default);
    Task CreateProfileStubAsync(Guid userId, CancellationToken ct = default);
    Task SetApplicationUnderReviewAsync(Guid userId, CancellationToken ct = default);
    Task ActivateProfileAsync(Guid userId, CancellationToken ct = default);
    Task<(string email, Guid applicationId)> ValidateInviteTokenAsync(string token, CancellationToken ct = default);
    Task<PagedResult<ApplicationSummaryDto>> GetApplicationsAsync(string? status, int page, int pageSize, CancellationToken ct = default);
    Task<ApplicationDetailDto> GetApplicationDetailAsync(Guid applicationId, CancellationToken ct = default);
    Task<string> PreApproveApplicationAsync(Guid applicationId, string? notes, CancellationToken ct = default);
    Task ApproveApplicationAsync(Guid applicationId, string? notes, CancellationToken ct = default);
    Task RejectApplicationAsync(Guid applicationId, string reason, CancellationToken ct = default);
    Task RequestRevisionAsync(Guid applicationId, string notes, CancellationToken ct = default);
}
