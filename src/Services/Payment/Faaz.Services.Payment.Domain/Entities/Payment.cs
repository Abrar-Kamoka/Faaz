using Faaz.SharedKernel.Entities;
using static Faaz.Services.Payment.Domain.PaymentEnums;

namespace Faaz.Services.Payment.Domain.Entities;

public class Payment : BaseSoftDeleteModel
{
    public Payment()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid           BookingId             { get; set; }
    public Guid           StudentUserId         { get; set; }
    public Guid           ConsultantUserId      { get; set; }
    public string         StripePaymentIntentId { get; set; } = string.Empty;
    public string?        StripeChargeId        { get; set; }
    public string?        StripeCustomerId      { get; set; }
    public decimal        Amount                { get; set; }
    public decimal        PlatformFee           { get; set; }
    public decimal        ConsultantPayout      { get; set; }
    public string         Currency              { get; set; } = "gbp";
    public PaymentStatus  Status                { get; set; } = PaymentStatus.Pending;
    public string?        PromoCodeUsed         { get; set; }
    public decimal        DiscountAmount        { get; set; } = 0m;
    public string?        FailureMessage        { get; set; }
    public string?        Metadata              { get; set; }
    public string?        Remarks               { get; set; }
    public string?        ExtraField1           { get; set; }
    public string?        ExtraField2           { get; set; }

    public ICollection<Refund> Refunds { get; set; } = [];
}
