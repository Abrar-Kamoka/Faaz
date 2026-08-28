using Faaz.Services.Administration.Domain.Entities;
using Faaz.Services.Administration.Infrastructure.HttpClients;
using Faaz.Services.Administration.Infrastructure.Interfaces;

namespace Faaz.Services.Administration.WebHost.Features.AuditLog;

// AdminActionLog stores only raw ids — AdminUserId, EntityType + EntityId — which is why the audit
// log used to render as "e4c2ae07 · ConsultantProfile#b4453e0a". This turns that into names a
// non-technical admin can actually read, by calling the same internal clients each entity type's
// own admin screen already uses. Best-effort throughout: a failed/unavailable lookup just falls back
// to a shorter id or a plain type label instead of failing the whole audit log page.
public sealed record EnrichedAuditLogEntry(
    Guid Id,
    Guid AdminUserId,
    string AdminName,
    int Action,
    string EntityType,
    Guid EntityId,
    string? EntityDisplayName,
    string? Notes,
    DateTime PerformedAt);

public interface IAuditLogEnricher
{
    Task<IReadOnlyList<EnrichedAuditLogEntry>> EnrichAsync(IReadOnlyList<AdminActionLog> logs, CancellationToken ct);
}

internal sealed class AuditLogEnricher(
    IAdminIdentityClient identityClient,
    IAdminConsultantClient consultantClient,
    IAdminBookingClient bookingClient,
    IAdminPaymentClient paymentClient,
    IUniversityServices universityServices,
    ISubjectServices subjectServices) : IAuditLogEnricher
{
    public async Task<IReadOnlyList<EnrichedAuditLogEntry>> EnrichAsync(IReadOnlyList<AdminActionLog> logs, CancellationToken ct)
    {
        // Resolve each distinct admin once — a page of 50 rows is often just a handful of admins.
        var adminNames = new Dictionary<Guid, string>();
        foreach (var adminId in logs.Select(l => l.AdminUserId).Distinct())
            adminNames[adminId] = await ResolveAdminNameAsync(adminId, ct);

        var result = new List<EnrichedAuditLogEntry>(logs.Count);
        foreach (var log in logs)
        {
            var entityName = await TryResolveEntityNameAsync(log.EntityType, log.EntityId, ct);
            result.Add(new EnrichedAuditLogEntry(
                log.Id, log.AdminUserId, adminNames[log.AdminUserId], (int)log.Action,
                log.EntityType, log.EntityId, entityName, log.Notes, log.PerformedAt));
        }
        return result;
    }

    private async Task<string> ResolveAdminNameAsync(Guid adminId, CancellationToken ct)
    {
        try
        {
            var user = await identityClient.GetUserByIdAsync(adminId, ct);
            return user is not null ? $"{user.FirstName} {user.LastName}" : adminId.ToString()[..8];
        }
        catch
        {
            return adminId.ToString()[..8];
        }
    }

    private async Task<string?> TryResolveEntityNameAsync(string entityType, Guid entityId, CancellationToken ct)
    {
        try
        {
            switch (entityType)
            {
                case "User":
                    var user = await identityClient.GetUserByIdAsync(entityId, ct);
                    return user is not null ? $"{user.FirstName} {user.LastName}" : null;

                // ConsultantProfile audit rows are keyed by userId, not the profile's own Id —
                // matches GetProfileByIdAsync's parameter and how every write site logs it
                // (e.g. FeaturedConsultantsAdminController.Feature/Unfeature).
                case "ConsultantProfile":
                    var profile = await consultantClient.GetProfileByIdAsync(entityId, ct);
                    return profile?.FullLegalName;

                case "ConsultantApplication":
                    var application = await consultantClient.GetApplicationByIdAsync(entityId, ct);
                    return application is not null ? $"{application.FirstName} {application.LastName}" : null;

                case "Booking":
                    var booking = await bookingClient.GetBookingByIdAsync(entityId, ct);
                    return booking?.Reference;

                case "Transaction":
                    var transaction = await paymentClient.GetTransactionByIdAsync(entityId, ct);
                    return transaction?.Reference;

                case "PromoCode":
                    var promoCode = await paymentClient.GetPromoCodeByIdAsync(entityId, ct);
                    return promoCode?.Code;

                // Owned directly by this service's own database — no internal HTTP call needed.
                case "University":
                    var university = await universityServices.GetByIdAsync(entityId, ct);
                    return university?.Name;

                case "Subject":
                    var subject = await subjectServices.GetByIdAsync(entityId, ct);
                    return subject?.Name;

                // PlatformConfig/Announcement/NotificationTemplate/Role have no single-item lookup
                // wired up yet — fall through to null, and the caller shows just the type label.
                default:
                    return null;
            }
        }
        catch
        {
            return null;
        }
    }
}
