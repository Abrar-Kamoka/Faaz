namespace Faaz.Services.Booking.WebHost.Features.Reviews.DTOs;

public class ReviewDto
{
    public Guid    Id                  { get; set; }
    public Guid    BookingId           { get; set; }
    public Guid    StudentUserId       { get; set; }
    public Guid    ConsultantProfileId { get; set; }
    public int     Rating              { get; set; }
    public string? ReviewText          { get; set; }
    public bool    IsPublic            { get; set; }
    public DateTime CreatedAt          { get; set; }
}

public class ReviewSummaryDto
{
    public Guid    ConsultantProfileId { get; set; }
    public decimal AverageRating       { get; set; }
    public int     TotalCount          { get; set; }
    public int     FiveStarCount       { get; set; }
    public int     FourStarCount       { get; set; }
    public int     ThreeStarCount      { get; set; }
    public int     TwoStarCount        { get; set; }
    public int     OneStarCount        { get; set; }
}

public class SubmitReviewDto { public int Rating { get; set; } public string? ReviewText { get; set; } }
public class SetVisibilityDto { public bool IsPublic { get; set; } }

public class AdminReviewDto
{
    public Guid    Id                  { get; set; }
    public Guid    BookingId           { get; set; }
    public Guid    StudentUserId       { get; set; }
    public Guid    ConsultantProfileId { get; set; }
    public int     Rating              { get; set; }
    public string? ReviewText          { get; set; }
    public bool    IsPublic            { get; set; }
    public bool    IsDeleted           { get; set; }
    public DateTime CreatedAt          { get; set; }
}
