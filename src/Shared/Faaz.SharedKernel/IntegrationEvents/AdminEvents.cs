namespace Faaz.SharedKernel.IntegrationEvents;

public record ConsultantSuspendedByAdminEvent(
    Guid ConsultantUserId,
    Guid AdminId,
    string Reason);

public record ConsultantRestoredByAdminEvent(
    Guid ConsultantUserId,
    Guid AdminId);

public record UserDeactivatedByAdminEvent(
    Guid UserId,
    Guid AdminId,
    string Reason);

public record UserReactivatedByAdminEvent(
    Guid UserId,
    Guid AdminId);

public record StaffCreatedEvent(
    Guid StaffUserId,
    Guid CreatedByAdminId,
    string RoleName);

public record StaffRoleChangedEvent(
    Guid StaffUserId,
    Guid OldRoleId,
    Guid NewRoleId,
    Guid ChangedByAdminId);

public record RolePermissionsChangedEvent(
    Guid RoleId,
    Guid ChangedByAdminId);

public record ReferenceRequestApprovedEvent(
    Guid RequestedByUserId,
    string EntityTypeName,
    string ProposedName);

public record ReferenceRequestRejectedEvent(
    Guid RequestedByUserId,
    string EntityTypeName,
    string ProposedName,
    string? ReviewNotes);
