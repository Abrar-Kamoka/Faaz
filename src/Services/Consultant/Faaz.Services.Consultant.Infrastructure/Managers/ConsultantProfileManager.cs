using Faaz.Services.Consultant.Domain;
using Faaz.Services.Consultant.Domain.Entities;
using Faaz.Services.Consultant.Infrastructure.DatabaseContext;
using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Faaz.SharedKernel.SharedEnums;

namespace Faaz.Services.Consultant.Infrastructure.Managers;

internal sealed class ConsultantProfileManager : IConsultantProfileServices
{
    private readonly ConsultantDbContext _db;

    public ConsultantProfileManager(ConsultantDbContext db)
    {
        _db = db;
    }

    // "Lean" — skips SessionTypes/AvailabilitySlots, but still includes Subjects/Services: those
    // used to be plain array columns (always loaded, no Include needed) that ProfileCompletenessChecker
    // reads for HasExpertise; now that they're join-table collections, TryAutoActivateAsync (called
    // from every profile-section command, including the scalar-only ones that use this fetch) would
    // silently see them as always-empty without this.
    public Task<ConsultantProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return _db.ConsultantProfiles
            .Include(p => p.Subjects)
            .Include(p => p.Services)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    // Full — includes SessionTypes + AvailabilitySlots + the expertise catalog references
    // (Services/Subjects/Universities). AsNoTracking for reads; tracking for writes.
    public Task<ConsultantProfile?> GetByUserIdWithCollectionsAsync(Guid userId, CancellationToken ct)
    {
        return _db.ConsultantProfiles
            .Include(p => p.SessionTypes)
            .Include(p => p.AvailabilitySlots)
            .Include(p => p.Services)
            .Include(p => p.Subjects)
            .Include(p => p.Universities)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    public Task<ConsultantProfile?> GetByIdWithCollectionsAsync(Guid profileId, CancellationToken ct)
    {
        return _db.ConsultantProfiles
            .Include(p => p.SessionTypes)
            .Include(p => p.AvailabilitySlots)
            .Include(p => p.Services)
            .Include(p => p.Subjects)
            .Include(p => p.Universities)
            .FirstOrDefaultAsync(p => p.Id == profileId, ct);
    }

    public Task<ConsultantProfile?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct)
    {
        return _db.ConsultantProfiles.FirstOrDefaultAsync(p => p.ApplicationId == applicationId, ct);
    }

    // Must include SessionTypes/AvailabilitySlots — this is the fetch behind the Stripe
    // "account.updated" webhook path (see ConsultantInternalApiController.SetStripeStatus), which
    // runs TryAutoActivateAsync straight after. Without these included, ProfileCompletenessChecker
    // sees empty collections regardless of what's actually saved and pricing/availability always
    // fail, so a profile can never auto-activate on the Stripe step even once genuinely complete.
    public Task<ConsultantProfile?> GetByStripeAccountIdAsync(string stripeAccountId, CancellationToken ct)
    {
        return _db.ConsultantProfiles
            .Include(p => p.SessionTypes)
            .Include(p => p.AvailabilitySlots)
            .Include(p => p.Services)
            .Include(p => p.Subjects)
            .FirstOrDefaultAsync(p => p.StripeAccountId == stripeAccountId, ct);
    }

    public Task<bool> ExistsForUserAsync(Guid userId, CancellationToken ct)
    {
        return _db.ConsultantProfiles.AnyAsync(p => p.UserId == userId, ct);
    }

    public async Task AddAsync(ConsultantProfile profile, CancellationToken ct)
    {
        await _db.ConsultantProfiles.AddAsync(profile, ct);
    }

    public async Task AddSessionTypeAsync(ConsultantSessionType sessionType, CancellationToken ct)
    {
        await _db.ConsultantSessionTypes.AddAsync(sessionType, ct);
    }

    public async Task AddAvailabilitySlotAsync(ConsultantAvailabilitySlot slot, CancellationToken ct)
    {
        await _db.ConsultantAvailabilitySlots.AddAsync(slot, ct);
    }

    public async Task<(IReadOnlyList<ConsultantProfile> Items, int Total)> GetAllActiveAsync(
        Guid? subjectId, string? search, Guid? serviceId, StudyLevel? studyLevel, bool? verifiedOnly,
        int page, int pageSize, CancellationToken ct)
    {
        var query = _db.ConsultantProfiles
            .Include(p => p.SessionTypes.Where(s => !s.IsDeleted && s.IsActive))
            .Include(p => p.AvailabilitySlots.Where(s => !s.IsDeleted && !s.IsBlockedDate))
            .Include(p => p.Subjects)
            .Where(p => p.IsActive && !p.IsDeleted)
            .AsNoTracking();

        // Real EF-translatable filters (SQL EXISTS/IN) — the old version materialized the entire
        // active-consultant table into memory before filtering, because SubjectAreas/ServicesOffered
        // were JSON-blob columns LINQ-to-SQL can't see inside. Now that they're join tables, the
        // filter runs in the database.
        if (subjectId.HasValue)
            query = query.Where(p => p.Subjects.Any(s => s.SubjectId == subjectId.Value));

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Subject names aren't local anymore (they live in Administration's catalog) — matching
            // by subject text would need a cross-service lookup, so free-text search now covers only
            // the consultant's own fields; use subjectId for an exact subject filter.
            var term = search.Trim();
            query = query.Where(p =>
                p.DisplayName.Contains(term) ||
                p.CurrentRole.Contains(term) ||
                p.Institution.Contains(term));
        }

        if (serviceId.HasValue)
            query = query.Where(p => p.Services.Any(s => s.ServiceId == serviceId.Value));

        if (studyLevel.HasValue)
            query = query.Where(p => p.StudyLevelsOffered.Contains(studyLevel.Value));

        if (verifiedOnly == true)
            query = query.Where(p => p.IsFeatured);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(p => p.DisplayName)
                                .Skip((page - 1) * pageSize).Take(pageSize)
                                .ToListAsync(ct);
        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<ConsultantProfile> Items, int Total)> GetAllForAdminAsync(
        int page, int pageSize, CancellationToken ct)
    {
        var query = _db.ConsultantProfiles
            .IgnoreQueryFilters()
            .Include(p => p.Application)
            .Include(p => p.SessionTypes)
            .AsNoTracking();

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public Task<ConsultantProfile?> GetByUserIdWithApplicationAsync(Guid userId, CancellationToken ct)
        => _db.ConsultantProfiles
              .Include(p => p.Application)
              .Include(p => p.SessionTypes)
              .Include(p => p.Subjects)
              .FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task<(IReadOnlyList<ConsultantProfile> Items, int Total)> GetFeaturedAsync(
        int page, int pageSize, CancellationToken ct)
    {
        var query = _db.ConsultantProfiles
            .IgnoreQueryFilters()
            .Include(p => p.Application)
            .Include(p => p.SessionTypes)
            .Where(p => p.IsFeatured && !p.IsDeleted)
            .AsNoTracking();

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    // Call BEFORE SaveChangesAsync — marks profile active in memory so the caller's save
    // persists both the profile change and the activation in a single DB round-trip.
    public async Task<bool> TryAutoActivateAsync(ConsultantProfile profile, CancellationToken ct)
    {
        if (profile.IsActive) return false;

        var check = ProfileCompletenessChecker.Evaluate(profile);
        if (!check.IsComplete) return false;

        profile.IsProfileComplete = true;
        profile.IsActive          = true;

        var application = await _db.ConsultantApplications
            .FirstOrDefaultAsync(a => a.UserId == profile.UserId, ct);

        if (application is not null)
            application.ApplicationStatus = ConsultantApplicationStatus.Active;

        return true;
    }
}
