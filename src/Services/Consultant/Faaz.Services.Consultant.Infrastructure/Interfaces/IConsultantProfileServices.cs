using Faaz.Services.Consultant.Domain.Entities;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.Infrastructure.Interfaces;

public interface IConsultantProfileServices
{
    // Lean fetch — no child collections. Use for scalar-only writes (personal-info, expertise, bio, call-preferences).
    Task<ConsultantProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    // Full fetch with SessionTypes + AvailabilitySlots. Use for pricing/availability writes and all reads.
    Task<ConsultantProfile?> GetByUserIdWithCollectionsAsync(Guid userId, CancellationToken ct = default);

    // Fetch by profile ID (not userId). Used by internal service-to-service calls.
    Task<ConsultantProfile?> GetByIdWithCollectionsAsync(Guid profileId, CancellationToken ct = default);

    // List all active profiles for student browsing.
    Task<(IReadOnlyList<ConsultantProfile> Items, int Total)> GetAllActiveAsync(
        Guid? subjectId, string? search, Guid? serviceId, StudyLevel? studyLevel, bool? verifiedOnly,
        int page, int pageSize, CancellationToken ct = default);

    Task<ConsultantProfile?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct = default);
    Task<ConsultantProfile?> GetByStripeAccountIdAsync(string stripeAccountId, CancellationToken ct = default);
    Task<bool> ExistsForUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(ConsultantProfile profile, CancellationToken ct = default);
    Task AddSessionTypeAsync(ConsultantSessionType sessionType, CancellationToken ct = default);
    Task AddAvailabilitySlotAsync(ConsultantAvailabilitySlot slot, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    // Checks profile completeness and, if 100%, activates both the profile and the application.
    // Returns true when activation happened (first time only).
    // Must be called BEFORE SaveChangesAsync so activation is part of the same save.
    Task<bool> TryAutoActivateAsync(ConsultantProfile profile, CancellationToken ct = default);

    // Admin-only: all profiles including inactive, with Application loaded.
    Task<(IReadOnlyList<ConsultantProfile> Items, int Total)> GetAllForAdminAsync(int page, int pageSize, CancellationToken ct = default);

    // Fetch with Application + SessionTypes (for admin detail / suspend-restore).
    Task<ConsultantProfile?> GetByUserIdWithApplicationAsync(Guid userId, CancellationToken ct = default);

    // Admin: featured consultants list.
    Task<(IReadOnlyList<ConsultantProfile> Items, int Total)> GetFeaturedAsync(int page, int pageSize, CancellationToken ct = default);
}
