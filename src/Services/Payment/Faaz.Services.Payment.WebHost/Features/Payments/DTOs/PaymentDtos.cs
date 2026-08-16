namespace Faaz.Services.Payment.WebHost.Features.Payments.DTOs;

public class CreatePaymentIntentDto
{
    public Guid    BookingId     { get; set; }
    public string? PromoCode     { get; set; }
}

public class PaymentIntentResultDto
{
    public Guid    PaymentId        { get; set; }
    public string  ClientSecret     { get; set; } = "";
    public string  IntentId         { get; set; } = "";
    public decimal Amount           { get; set; }
    public decimal PlatformFee      { get; set; }
    public decimal ConsultantPayout { get; set; }
    public string  Currency         { get; set; } = "gbp";
}

public class PaymentStatusDto
{
    public Guid   Id                    { get; set; }
    public Guid   BookingId             { get; set; }
    public string Status                { get; set; } = "";
    public decimal Amount               { get; set; }
    public decimal PlatformFee          { get; set; }
    public decimal ConsultantPayout     { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? FailureMessage       { get; set; }
    public DateTime CreatedAt           { get; set; }
}

public class ConsultantEarningsDto
{
    public Guid    ConsultantUserId { get; set; }
    public decimal TotalEarnings    { get; set; }
    public decimal PendingPayout    { get; set; }
    public int     TotalSessions    { get; set; }
}

public class PayoutDto
{
    public Guid      Id              { get; set; }
    public Guid      BookingId       { get; set; }
    public decimal   Amount          { get; set; }
    public string    Status          { get; set; } = "";
    public DateTime? ScheduledAt     { get; set; }
    public DateTime? ReleasedAt      { get; set; }
    public DateTime  CreatedAt       { get; set; }
}

public class PaymentHistoryItemDto
{
    public Guid     Id            { get; set; }
    public Guid     BookingId     { get; set; }
    public decimal  Amount        { get; set; }
    public decimal  DiscountAmount { get; set; }
    public string?  PromoCodeUsed { get; set; }
    public string   Status        { get; set; } = "";
    public DateTime CreatedAt     { get; set; }
}

public class PromoCodeValidationDto
{
    public bool    IsValid          { get; set; }
    public string? Code             { get; set; }
    public string? DiscountType     { get; set; }
    public decimal DiscountValue    { get; set; }
    public decimal DiscountAmount   { get; set; }
    public decimal FinalAmount      { get; set; }
    public string? InvalidReason    { get; set; }
}
