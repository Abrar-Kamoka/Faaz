namespace Faaz.SharedKernel.IntegrationEvents;

public record StudentRegisteredEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string VerificationToken);

// Fired when a consultant verifies their email.
// The Consultant service consumer handles BOTH setting status to SettingUpProfile
// AND creating the profile stub in one consumer.
public record ConsultantEmailVerifiedEvent(Guid UserId);

public record ConsultantApprovedEvent(
    Guid UserId,
    string Email,
    string FirstName);

public record ConsultantRejectedEvent(
    Guid UserId,
    string Email,
    string Reason);

public record ConsultantRevisionRequestedEvent(
    Guid UserId,
    string Email,
    string Notes);

public record SendVerificationEmailEvent(
    string To,
    string FirstName,
    string VerificationToken);

public record SendPasswordResetEmailEvent(
    string To,
    string FirstName,
    string ResetToken);
