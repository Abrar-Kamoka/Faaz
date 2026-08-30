namespace Faaz.Services.Student.Domain.Entities;

// Plain many-to-many link to Administration's Subject catalog (cross-service Guid reference).
public class StudentProfileTargetSubject
{
    public Guid StudentProfileId { get; set; }
    public Guid SubjectId        { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
}
