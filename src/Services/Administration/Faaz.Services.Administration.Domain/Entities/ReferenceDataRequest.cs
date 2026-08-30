using Faaz.SharedKernel.Entities;

namespace Faaz.Services.Administration.Domain.Entities;

public enum ReferenceEntityType
{
    University = 1,
    Programme  = 2,
    Subject    = 3,
    Service    = 4
}

public enum ReferenceRequestStatus
{
    Pending  = 1,
    Approved = 2,
    Rejected = 3
}

// The "can't find it? request it" escape hatch for the wizard — keeps the catalog free-text-free
// (no fallback to typing a raw string) while still unblocking a student/consultant whose real
// university/programme genuinely isn't in the catalog yet. Approving a request creates the real,
// verified entity; nothing becomes selectable until an admin has looked at it.
public class ReferenceDataRequest : BaseSoftDeleteModel
{
    public ReferenceDataRequest()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid                   RequestedByUserId { get; set; }
    public string                 RequestedByRole   { get; set; } = string.Empty; // "Student" / "Consultant"
    public ReferenceEntityType    EntityType        { get; set; }
    public string                 ProposedName      { get; set; } = string.Empty;
    public string?                Details           { get; set; }
    public ReferenceRequestStatus Status            { get; set; } = ReferenceRequestStatus.Pending;
    public Guid?                  ReviewedByAdminUserId { get; set; }
    public string?                ReviewNotes       { get; set; }
    public DateTime?              ReviewedAt        { get; set; }
    // Set on approval for University/Subject/Service (auto-created, inactive pending polish) —
    // null for Programme requests, which are never auto-created (see ReferenceRequestsAdminController).
    public Guid?                  CreatedEntityId   { get; set; }
}
