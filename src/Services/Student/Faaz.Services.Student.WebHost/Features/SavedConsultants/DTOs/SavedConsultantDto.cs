namespace Faaz.Services.Student.WebHost.Features.SavedConsultants.DTOs;

public class SessionTypeSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int DurationMinutes { get; set; }
    public decimal PriceGbp { get; set; }
}

public class SavedConsultantDto
{
    public Guid UserId { get; set; }
    public Guid ProfileId { get; set; }
    public string DisplayName { get; set; } = "";
    public string? ProfessionalPhotoUrl { get; set; }
    public string CurrentRole { get; set; } = "";
    public string Institution { get; set; } = "";
    public bool IsVerified { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string[] SubjectAreas { get; set; } = [];
    public List<SessionTypeSummaryDto> SessionTypes { get; set; } = [];
    public bool IsAvailableThisWeek { get; set; }
}
