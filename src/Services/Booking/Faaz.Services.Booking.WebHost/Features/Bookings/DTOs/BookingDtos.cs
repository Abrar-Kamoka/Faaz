namespace Faaz.Services.Booking.WebHost.Features.Bookings.DTOs;

public class BookingDetailDto
{
    public Guid Id { get; set; }
    public int SrNo { get; set; }
    public Guid StudentUserId { get; set; }
    public string? StudentName { get; set; }
    public Guid ConsultantUserId { get; set; }
    public string? ConsultantName { get; set; }
    public Guid ConsultantProfileId { get; set; }
    public Guid SessionTypeId { get; set; }
    public string SessionTypeName { get; set; } = "";
    public int DurationMinutes { get; set; }
    public decimal SessionPriceGbp { get; set; }
    public decimal TotalChargedGbp { get; set; }
    public decimal PlatformCommissionGbp { get; set; }
    public string CallType { get; set; } = "";
    public DateTime ScheduledStartUtc { get; set; }
    public DateTime ScheduledEndUtc { get; set; }
    public string StudentTimezone { get; set; } = "";
    public string? SessionBrief { get; set; }
    public string Status { get; set; } = "";
    public DateTime? CreatedAt { get; set; }
    public DateTime? SlotReservedUntilUtc { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public int? RefundPercentage { get; set; }
    public string? DisputeReason { get; set; }
    public string? DisputeResolution { get; set; }
    public string? DisputeResolutionNote { get; set; }
    public DateTime? DisputeResolvedAt { get; set; }
    public SessionDto? Session { get; set; }
}

public class BookingListDto
{
    public Guid Id { get; set; }
    public int SrNo { get; set; }
    public string Status { get; set; } = "";
    public string SessionTypeName { get; set; } = "";
    public int DurationMinutes { get; set; }
    public decimal SessionPriceGbp { get; set; }
    public decimal TotalChargedGbp { get; set; }
    public string CallType { get; set; } = "";
    public DateTime ScheduledStartUtc { get; set; }
    public DateTime ScheduledEndUtc { get; set; }
    public string StudentTimezone { get; set; } = "";
    public bool HasReview { get; set; }
    public int? RefundPercentage { get; set; }
    public Guid ConsultantUserId { get; set; }
    public Guid ConsultantProfileId { get; set; }
    public DateTime? SlotReservedUntilUtc { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class SessionDto
{
    public Guid Id { get; set; }
    public string LiveKitRoomName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? ActualStartUtc { get; set; }
    public DateTime? ActualEndUtc { get; set; }
    public int? ActualDurationSeconds { get; set; }
    public decimal? CompletionPct { get; set; }
}

public class CreateBookingDto
{
    public Guid ConsultantProfileId { get; set; }
    public Guid SessionTypeId { get; set; }
    public int CallType { get; set; }
    public DateTime ScheduledStartUtc { get; set; }
    public string StudentTimezone { get; set; } = "";
    public string? SessionBrief { get; set; }
    public Guid? PromoCodeId { get; set; }
}

public class CancelBookingDto { public string? Reason { get; set; } }

public class RescheduleBookingDto { public DateTime NewScheduledStartUtc { get; set; } }

public class AdminDecisionDto { public string? Reason { get; set; } }

public class SubmitRefundAppealDto { public string Reason { get; set; } = string.Empty; }

public class FileDisputeDto { public string Reason { get; set; } = string.Empty; }

public class ResolveDisputeDto
{
    public string Resolution { get; set; } = string.Empty; // "favour_student" | "favour_consultant" | "no_action"
    public string Note { get; set; } = string.Empty;
}

public class OverrideStatusDto
{
    public int Status { get; set; }
    public string? Reason { get; set; }
}

public class RefundAppealDto
{
    public Guid Id { get; set; }
    public int SrNo { get; set; }
    public Guid BookingId { get; set; }
    public string Status { get; set; } = "";
    public decimal RequestedAmountGbp { get; set; }
    public string Reason { get; set; } = "";
    public DateTime SubmittedAt { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class RefundAppealAdminDto : RefundAppealDto
{
    public Guid StudentUserId { get; set; }
    public decimal BookingTotalGbp { get; set; }
    public DateTime BookingScheduledStart { get; set; }
}
