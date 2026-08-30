namespace Faaz.Services.Consultant.Domain.Entities;

// Plain many-to-many link to Administration's University catalog (cross-service Guid reference).
// IsVerified/VerifiedAt/VerifiedByAdminUserId are forward-looking room for a future claim-
// verification workflow (documents / alumni-email match) — not built yet, but free to add now
// since a consultant claiming affiliation with a real, catalog-verified university is only half
// of "otherwise they can claim to our application"; confirming they actually attended is the rest.
public class ConsultantProfileUniversity
{
    public Guid     ConsultantProfileId    { get; set; }
    public Guid     UniversityId           { get; set; }
    public bool     IsVerified             { get; set; } = false;
    public DateTime? VerifiedAt            { get; set; }
    public Guid?     VerifiedByAdminUserId { get; set; }

    public ConsultantProfile Profile { get; set; } = null!;
}
