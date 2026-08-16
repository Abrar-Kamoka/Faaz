using Faaz.Services.Notification.Domain.Entities;
using Faaz.Services.Notification.Infrastructure.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Faaz.Services.Notification.Domain.NotificationEnums;

namespace Faaz.Services.Notification.WebHost.Seeding;

public static class NotificationTemplateSeeder
{
    // Mirrors the current hardcoded text in each consumer at the time this was introduced — editing
    // a row here from Notification Templates changes what gets sent going forward. Consumers not
    // yet migrated to read from the store (the HTML email ones) are left out; they keep using their
    // own hardcoded text until a follow-up pass wires them up the same way.
    private static readonly (string Key, NotificationChannel Channel, string Subject, string Body, string Description)[] Catalog =
    [
        ("BookingConfirmedEvent", NotificationChannel.InApp,
            "Booking confirmed", "Your booking has been confirmed. Session starts {{SessionStartUtc}} UTC.",
            "Sent to the student when a consultant accepts a booking. Placeholders: SessionStartUtc"),
        ("BookingCancelledEvent", NotificationChannel.InApp,
            "Booking cancelled", "Booking {{BookingId}} was cancelled by {{CancelledBy}}. Reason: {{Reason}}.{{RefundNote}}",
            "Sent when either party cancels a booking. Placeholders: BookingId, CancelledBy, Reason, RefundNote"),
        ("BookingDisputedEvent", NotificationChannel.InApp,
            "Booking dispute raised", "A dispute has been raised for booking {{BookingId}}. Reason: {{Reason}}",
            "Sent to the consultant when a student files a dispute. Placeholders: BookingId, Reason"),
        ("DisputeResolvedEvent", NotificationChannel.InApp,
            "Dispute resolved", "The dispute for booking {{BookingId}} has been resolved: {{Resolution}}. {{Note}}",
            "Sent to both parties once an admin resolves a dispute. Placeholders: BookingId, Resolution, Note"),
        ("BookingRequestReceivedEvent", NotificationChannel.InApp,
            "New booking request received", "A new booking request has been received for session starting {{SlotStartUtc}} UTC.",
            "Sent to the consultant when a student requests a booking. Placeholders: SlotStartUtc"),
        ("BookingRescheduledEvent", NotificationChannel.InApp,
            "Booking rescheduled — please re-confirm", "The student moved this booking to {{NewStartUtc}} UTC (was {{OldStartUtc}} UTC). Please review and confirm the new time.",
            "Sent to the consultant when a student reschedules. Placeholders: NewStartUtc, OldStartUtc"),
        ("SessionReminderEvent", NotificationChannel.InApp,
            "Session reminder — {{ReminderType}}", "Your session starts {{SessionStartUtc}} UTC. Reminder type: {{ReminderType}}.",
            "Sent ahead of an upcoming session. Placeholders: SessionStartUtc, ReminderType"),
        ("SessionNoShowEvent", NotificationChannel.InApp,
            "Session no-show detected", "Session for booking {{BookingId}} ended without {{Who}} joining.",
            "Sent when a scheduled session ends with a missing participant. Placeholders: BookingId, Who"),
        ("PayoutReleasedEvent", NotificationChannel.InApp,
            "Payout released", "Your payout of £{{Amount}} for booking {{BookingId}} has been released.",
            "Sent to the consultant once a payout is released. Placeholders: Amount, BookingId"),
        ("PayoutFailedEvent", NotificationChannel.InApp,
            "Payout failed", "Your payout of £{{Amount}} for booking {{BookingId}} failed. Reason: {{FailureReason}}",
            "Sent to the consultant if a payout fails. Placeholders: Amount, BookingId, FailureReason"),
        ("RefundIssuedEvent", NotificationChannel.InApp,
            "Refund issued", "A refund of £{{TotalRefunded}} has been issued for booking {{BookingId}}. Reason: {{Reason}}",
            "Sent to the student when a refund is issued. Placeholders: TotalRefunded, BookingId, Reason"),
        ("RefundAppealApprovedEvent", NotificationChannel.InApp,
            "Refund appeal approved", "Your refund appeal for booking {{BookingId}} has been approved. A refund of £{{RefundAmountGbp}} will be processed.",
            "Sent to the student when their refund appeal is approved. Placeholders: BookingId, RefundAmountGbp"),
        ("ConsultantSuspendedByAdminEvent", NotificationChannel.InApp,
            "Account suspended", "Your consultant account has been suspended. Reason: {{Reason}}. Please contact support for assistance.",
            "Sent when an admin suspends a consultant. Placeholders: Reason"),
        ("ConsultantRestoredByAdminEvent", NotificationChannel.InApp,
            "Account restored", "Your consultant account has been restored and is now active.",
            "Sent when an admin restores a suspended consultant."),
        ("UserDeactivatedByAdminEvent", NotificationChannel.InApp,
            "Account deactivated", "Your account has been deactivated. Reason: {{Reason}}. Contact support if you believe this is a mistake.",
            "Sent when an admin deactivates any account. Placeholders: Reason"),
        ("UserReactivatedByAdminEvent", NotificationChannel.InApp,
            "Account reactivated", "Your account has been reactivated. Welcome back!",
            "Sent when an admin reactivates a deactivated account."),
        ("PaymentAuthorizedEvent", NotificationChannel.InApp,
            "Payment received", "£{{Amount}} held for your upcoming session.",
            "Sent to the student once their card is authorized (funds held, not yet captured). Placeholders: Amount"),
    ];

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        var db = services.GetRequiredService<NotificationDbContext>();

        var existingKeys = await db.NotificationTemplates.Select(t => t.Key).ToListAsync();
        var toAdd = Catalog.Where(c => !existingKeys.Contains(c.Key)).ToList();
        if (toAdd.Count == 0) return;

        foreach (var (key, channel, subject, body, description) in toAdd)
        {
            db.NotificationTemplates.Add(new NotificationTemplate
            {
                Key         = key,
                Channel     = channel,
                Subject     = subject,
                Body        = body,
                Description = description
            });
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Notification template seed: added {Count} template(s)", toAdd.Count);
    }
}
