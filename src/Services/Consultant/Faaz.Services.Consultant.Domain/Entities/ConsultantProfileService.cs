namespace Faaz.Services.Consultant.Domain.Entities;

// Plain many-to-many link to Administration's Service catalog (a Guid, not a local FK — the
// Service entity lives in a different service's database). No BaseEntity: a pure join row doesn't
// need its own Id/SrNo/audit columns.
public class ConsultantProfileService
{
    public Guid ConsultantProfileId { get; set; }
    public Guid ServiceId           { get; set; }

    public ConsultantProfile Profile { get; set; } = null!;
}
