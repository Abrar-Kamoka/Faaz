namespace Faaz.SharedKernel.IntegrationEvents;

// Fired the moment a student's onboarding wizard completes (all 4 steps done) — i.e. the first time
// they reach a fully set-up dashboard. Notification service uses this to post a one-time welcome
// message to their in-app notification center.
public record StudentOnboardingCompletedEvent(
    Guid UserId,
    string FirstName);
