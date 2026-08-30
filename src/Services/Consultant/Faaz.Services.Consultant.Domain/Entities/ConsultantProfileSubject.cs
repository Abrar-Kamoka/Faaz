namespace Faaz.Services.Consultant.Domain.Entities;

// Plain many-to-many link to Administration's Subject catalog (cross-service Guid reference).
public class ConsultantProfileSubject
{
    public Guid ConsultantProfileId { get; set; }
    public Guid SubjectId           { get; set; }

    public ConsultantProfile Profile { get; set; } = null!;
}
