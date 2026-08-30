namespace Faaz.Services.Student.Domain.Entities;

// Plain many-to-many link to Administration's University catalog (cross-service Guid reference).
public class StudentProfileTargetUniversity
{
    public Guid StudentProfileId { get; set; }
    public Guid UniversityId     { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
}
