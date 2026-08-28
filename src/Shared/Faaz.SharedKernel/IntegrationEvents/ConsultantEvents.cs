namespace Faaz.SharedKernel.IntegrationEvents;

// Fired the moment a consultant's setup wizard self-completes their profile (ConsultantProfileManager.
// TryAutoActivateAsync flips IsActive) — i.e. the first time they reach a fully active dashboard.
// Notification service uses this to post a one-time welcome message to their in-app notification
// center. Deliberately scoped to this one self-service completion path, not admin-driven activation
// (ActivateProfileCommand, InternalAdminConsultantsController) or the initial-approval activation in
// ConsultantApprovedConsumer — those aren't "the consultant's own first sight of their finished portal".
public record ConsultantProfileActivatedEvent(
    Guid UserId,
    string DisplayName);
