using static Faaz.Services.Consultant.Domain.ConsultantEnums;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;

public class ConsultantProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ApplicationId { get; set; }
    public string FullLegalName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfessionalPhotoUrl { get; set; }
    public string CurrentRole { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string? LinkedInUrl { get; set; }
    public int YearsOfExperience { get; set; }
    public StudyLevel[] StudyLevelsOffered { get; set; } = [];
    public string[] SubjectAreas { get; set; } = [];
    public string[] SpecialisedUniversities { get; set; } = [];
    public ServiceType[] ServicesOffered { get; set; } = [];
    public string? WrittenBio { get; set; }
    public string? IntroVideoUrl { get; set; }
    public string CallPreference { get; set; } = string.Empty;
    public int MinBookingNoticeHours { get; set; }
    public int MaxAdvanceBookingDays { get; set; }
    public bool IsProfileComplete { get; set; }
    public bool IsActive { get; set; }
    public List<SessionTypeDto> SessionTypes { get; set; } = [];
    public List<WeeklySlotDto> AvailabilitySlots { get; set; } = [];
    public List<BlockedDateDto> BlockedDates { get; set; } = [];
}

public class SessionTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int DurationMinutes { get; set; }
    public decimal PriceGbp { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class WeeklySlotDto
{
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

public class BlockedDateDto
{
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }
}

public class ConsultantProfileSummaryDto
{
    public Guid    UserId               { get; set; }
    public Guid    ProfileId            { get; set; }
    public string  DisplayName          { get; set; } = string.Empty;
    public string  CurrentRole          { get; set; } = string.Empty;
    public string  Institution          { get; set; } = string.Empty;
    public string? ProfessionalPhotoUrl { get; set; }
    public string[] SubjectAreas        { get; set; } = [];
    public int     YearsOfExperience    { get; set; }
    public decimal MinPriceGbp          { get; set; }
    public List<SessionTypeSummaryDto> SessionTypes { get; set; } = [];
}

public class SessionTypeSummaryDto
{
    public Guid    Id              { get; set; }
    public string  Name            { get; set; } = string.Empty;
    public int     DurationMinutes { get; set; }
    public decimal PriceGbp        { get; set; }
}
