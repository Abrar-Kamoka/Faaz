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

    public Task<ConsultantProfile?> GetByApplicationIdAsync(Guid applicationId, CancellationToken ct)
    {
        return _db.ConsultantProfiles.FirstOrDefaultAsync(p => p.ApplicationId == applicationId, ct);
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

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
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
