namespace Faaz.Services.Student.Domain.Entities;

// Plain many-to-many link to Administration's Programme catalog (cross-service Guid reference) —
// new capability: a student can now target a specific real programme, not just a loose
// university+subject pair.
public class StudentProfileTargetProgramme
{
    public Guid StudentProfileId { get; set; }
    public Guid ProgrammeId      { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
}
