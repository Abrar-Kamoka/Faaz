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

    // Lean — no child collections. For scalar-only writes.
    public Task<ConsultantProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return _db.ConsultantProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    // Full — includes SessionTypes + AvailabilitySlots. AsNoTracking for reads; tracking for writes.
    public Task<ConsultantProfile?> GetByUserIdWithCollectionsAsync(Guid userId, CancellationToken ct)
    {
        return _db.ConsultantProfiles
            .Include(p => p.SessionTypes)
            .Include(p => p.AvailabilitySlots)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    public Task<ConsultantProfile?> GetByIdWithCollectionsAsync(Guid profileId, CancellationToken ct)
    {
        return _db.ConsultantProfiles
            .Include(p => p.SessionTypes)
            .Include(p => p.AvailabilitySlots)
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
        string? subjectFilter, string? search, int? sessionType, int? studyLevel, bool? verifiedOnly,
        int page, int pageSize, CancellationToken ct)
    {
        var query = _db.ConsultantProfiles
            .Include(p => p.SessionTypes.Where(s => !s.IsDeleted && s.IsActive))
            .Include(p => p.AvailabilitySlots.Where(s => !s.IsDeleted && !s.IsBlockedDate))
            .Where(p => p.IsActive && !p.IsDeleted)
            .AsNoTracking();

        var all = await query.OrderBy(p => p.DisplayName).ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(subjectFilter))
        {
            var f = subjectFilter.Trim().ToLowerInvariant();
            all = all.Where(p => (p.SubjectAreas ?? []).Any(s => s.ToLowerInvariant().Contains(f))).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            all = all.Where(p =>
                p.DisplayName.ToLowerInvariant().Contains(term) ||
                p.CurrentRole.ToLowerInvariant().Contains(term) ||
                p.Institution.ToLowerInvariant().Contains(term) ||
                (p.SubjectAreas ?? []).Any(s => s.ToLowerInvariant().Contains(term))
            ).ToList();
        }

        if (sessionType.HasValue)
            all = all.Where(p => (p.ServicesOffered ?? []).Contains(sessionType.Value)).ToList();

        if (studyLevel.HasValue)
            all = all.Where(p => (p.StudyLevelsOffered ?? []).Contains(studyLevel.Value)).ToList();

        if (verifiedOnly == true)
            all = all.Where(p => p.IsFeatured).ToList();

        var total = all.Count;
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
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
              .FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public async Task<(IReadOnlyList<ConsultantProfile> Items, int Total)> GetFeaturedAsync(
        int page, int pageSize, CancellationToken ct)
    {
        var query = _db.ConsultantProfiles
            .IgnoreQueryFilters()
            .Include(p => p.Application)
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
