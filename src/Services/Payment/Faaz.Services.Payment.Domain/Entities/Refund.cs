using Faaz.SharedKernel.Entities;
using static Faaz.Services.Payment.Domain.PaymentEnums;

namespace Faaz.Services.Payment.Domain.Entities;

public class Refund : BaseSoftDeleteModel
{
    public Refund()
    {
        Id = RT.Comb.Provider.Sql.Create();
    }

    public Guid         PaymentId             { get; set; }
    public Guid         BookingId             { get; set; }
    public Guid         StudentUserId         { get; set; }
    public string?      StripeRefundId        { get; set; }
    public decimal      Amount                { get; set; }
    public string       Currency              { get; set; } = "gbp";
    public RefundStatus Status                { get; set; } = RefundStatus.Pending;
    public string       Reason                { get; set; } = string.Empty;
    public int          RefundPercentage      { get; set; }
    public bool         IsAppealRefund        { get; set; } = false;
    public Guid?        AppealId              { get; set; }
    public string?      FailureReason         { get; set; }
    public string?      Remarks               { get; set; }
    public string?      ExtraField1           { get; set; }
    public string?      ExtraField2           { get; set; }

    public Payment Payment { get; set; } = null!;
}
