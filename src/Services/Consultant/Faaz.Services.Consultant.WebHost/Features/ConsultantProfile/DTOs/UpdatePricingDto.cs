namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;

public class UpdatePricingDto
{
    public required List<UpsertSessionTypeDto> SessionTypes { get; set; }
}

public class UpsertSessionTypeDto
{
    public Guid? Id { get; set; }
    public required string Name { get; set; }
    public required int DurationMinutes { get; set; }
    public required decimal PriceGbp { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}
