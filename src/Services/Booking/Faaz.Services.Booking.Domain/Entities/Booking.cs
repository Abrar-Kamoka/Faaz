using Faaz.SharedKernel.Entities;
using static Faaz.Services.Booking.Domain.BookingEnums;

namespace Faaz.Services.Booking.Domain.Entities;

public class Booking : BaseSoftDeleteModel
{
    public Booking()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid      StudentUserId           { get; set; }
    public Guid      ConsultantUserId        { get; set; }
    public Guid      ConsultantProfileId     { get; set; }
    public Guid      SessionTypeId           { get; set; }
    public string    SessionTypeName         { get; set; } = string.Empty;
    public int       DurationMinutes         { get; set; }
    public decimal   SessionPriceGbp         { get; set; }
    public decimal   PlatformCommissionGbp   { get; set; }
    public decimal   PromoDiscountGbp        { get; set; } = 0m;
    public decimal   TotalChargedGbp         { get; set; }
    public CallType  CallType                { get; set; }
    public DateTime  ScheduledStartUtc       { get; set; }
    public DateTime  ScheduledEndUtc         { get; set; }
    public string    StudentTimezone         { get; set; } = string.Empty;
    public string?   SessionBrief            { get; set; }
    public BookingStatus Status              { get; set; } = BookingStatus.SlotReserved;
    public DateTime? SlotReservedUntilUtc    { get; set; }
    public string?   StripePaymentIntentId   { get; set; }
    public DateTime? AcceptedAt              { get; set; }
    public DateTime? ExpiresAt               { get; set; }
    public CancellationReason? CancellationReason { get; set; }
    public string?   CancellationNotes       { get; set; }
    public int?      RefundPercentage        { get; set; }
    public DateTime? CompletedAt             { get; set; }
    public DateTime? SettledAt               { get; set; }
    public string?   DisputeReason           { get; set; }
    public string?   DisputeResolution       { get; set; }
    public string?   DisputeResolutionNote   { get; set; }
    public DateTime? DisputeResolvedAt       { get; set; }
    public Guid?     PromoCodeId             { get; set; }
    public string?   Remarks                 { get; set; }
    public string?   ExtraField1             { get; set; }
    public string?   ExtraField2             { get; set; }

    public ICollection<BookingStatusHistory> StatusHistory { get; set; } = [];
    public Session?      Session      { get; set; }
    public Review?       Review       { get; set; }
    public RefundAppeal? RefundAppeal { get; set; }
}

