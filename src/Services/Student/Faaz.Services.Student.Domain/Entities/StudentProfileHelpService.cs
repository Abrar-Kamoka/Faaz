namespace Faaz.Services.Student.Domain.Entities;

// Plain many-to-many link to Administration's Service catalog (cross-service Guid reference) — the
// same shared vocabulary Consultant uses for ConsultantProfileService, so "what a student needs"
// and "what a consultant offers" are drawn from one taxonomy, not two independently-duplicated enums.
public class StudentProfileHelpService
{
    public Guid StudentProfileId { get; set; }
    public Guid ServiceId        { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
}
