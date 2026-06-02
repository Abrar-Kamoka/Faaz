namespace Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.DTOs;

public class ApplicationStatusDto
{
    public Guid ApplicationId { get; set; }
    public string Status { get; set; } = null!;
    public string? AdminNotes { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
