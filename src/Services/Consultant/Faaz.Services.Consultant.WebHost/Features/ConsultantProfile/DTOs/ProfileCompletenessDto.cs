namespace Faaz.Services.Consultant.WebHost.Features.ConsultantProfile.DTOs;

public class ProfileCompletenessDto
{
    public int Percentage { get; set; }
    public bool HasPersonalInfo { get; set; }
    public bool HasExpertise { get; set; }
    public bool HasBio { get; set; }
    public bool HasPricing { get; set; }
    public bool HasAvailability { get; set; }
    public bool HasPayoutAccount { get; set; }
    public bool IsComplete { get; set; }
}
