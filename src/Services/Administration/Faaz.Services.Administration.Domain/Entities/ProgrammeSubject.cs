namespace Faaz.Services.Administration.Domain.Entities;

// Plain many-to-many link — a joint-honours programme (e.g. "Economics and Politics") can span
// more than one Subject. No BaseEntity: a pure join row doesn't need its own Id/SrNo/audit columns.
public class ProgrammeSubject
{
    public Guid ProgrammeId { get; set; }
    public Guid SubjectId   { get; set; }

    public Programme Programme { get; set; } = null!;
    public Subject   Subject   { get; set; } = null!;
}
